using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using ExeBuilder.Core.Model;

namespace ExeBuilder.Core.Analysis
{
    /// <summary>
    /// Analisi statica di un progetto MSBuild, basata solo sui file del progetto:
    /// il file stesso, il Directory.Build.props che MSBuild importerebbe e gli Import locali.
    /// Non carica MSBuild: è veloce, deterministica e non richiede Visual Studio installato.
    /// </summary>
    public sealed class ProjectAnalyzer
    {
        private const int MaxImportDepth = 8;

        private static readonly string[] SupportedExtensions = { ".csproj", ".vbproj", ".fsproj" };

        // GUID dei progetti ASP.NET classici (Web Application / MVC).
        private static readonly string[] WebProjectTypeGuids =
        {
            "349c5851-65df-11da-9384-00065b846f21",
            "e3e379df-f4c6-4180-9b81-6769533abe47",
            "e53f8fea-eae0-44a6-8774-ffd645390401"
        };

        public static bool IsProjectFile(string path)
        {
            var extension = Path.GetExtension(path) ?? string.Empty;
            return SupportedExtensions.Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase));
        }

        /// <param name="projectPath">Percorso del .csproj/.vbproj/.fsproj.</param>
        /// <param name="configuration">Configurazione simulata (Release).</param>
        /// <param name="solutionPath">Solution di provenienza (valorizza $(SolutionDir)), opzionale.</param>
        public ProjectInfo Analyze(string projectPath, string configuration = "Release", string solutionPath = null)
        {
            if (!File.Exists(projectPath))
            {
                throw new FileNotFoundException("File di progetto non trovato.", projectPath);
            }

            var info = new ProjectInfo(projectPath);
            var root = LoadXml(info.FullPath).Root;
            if (root == null || root.Name.LocalName != "Project")
            {
                throw new InvalidDataException("Il file non è un progetto MSBuild: " + projectPath);
            }

            info.Sdk = DetectSdk(root);
            info.IsSdkStyle = info.Sdk != null;

            var context = new EvaluationContext(info);
            InitializeProperties(context.Properties, info, configuration, solutionPath);

            // MSBuild importa Directory.Build.props all'inizio (da Sdk.props o da Microsoft.Common.props).
            var directoryBuildProps = FindFileAbove(info.Directory, "Directory.Build.props");
            if (directoryBuildProps != null)
            {
                info.DirectoryBuildProps = directoryBuildProps;
                context.Properties.Set("DirectoryBuildPropsPath", directoryBuildProps);
                EvaluateFile(context, directoryBuildProps, LoadXml(directoryBuildProps).Root, 1);
            }

            EvaluateFile(context, info.FullPath, root, 0);

            FillProjectInfo(info, context);
            if (string.IsNullOrEmpty(solutionPath))
            {
                InferSolutionDir(info, context.HintPaths);
            }

            return info;
        }

        private static void InitializeProperties(PropertyBag properties, ProjectInfo info, string configuration, string solutionPath)
        {
            properties.SetGlobal("Configuration", configuration);
            properties.Set("MSBuildProjectFullPath", info.FullPath);
            properties.Set("MSBuildProjectDirectory", info.Directory);
            properties.Set("MSBuildProjectName", info.Name);
            properties.Set("MSBuildProjectFile", Path.GetFileName(info.FullPath));
            properties.Set("MSBuildProjectExtension", Path.GetExtension(info.FullPath));
            properties.Set("OS", "Windows_NT");

            if (!string.IsNullOrEmpty(solutionPath))
            {
                var solutionDir = Path.GetDirectoryName(Path.GetFullPath(solutionPath)) + Path.DirectorySeparatorChar;
                properties.SetGlobal("SolutionDir", solutionDir);
                properties.SetGlobal("SolutionPath", Path.GetFullPath(solutionPath));
                properties.SetGlobal("SolutionName", Path.GetFileNameWithoutExtension(solutionPath));
            }
        }

        private void EvaluateFile(EvaluationContext context, string filePath, XElement root, int depth)
        {
            if (root == null)
            {
                return;
            }

            var directory = Path.GetDirectoryName(filePath);
            context.Properties.Set("MSBuildThisFileDirectory", directory + Path.DirectorySeparatorChar);
            context.Properties.Set("MSBuildThisFileFullPath", filePath);
            EvaluateChildren(context, filePath, root, depth);
        }

        private void EvaluateChildren(EvaluationContext context, string filePath, XElement parent, int depth)
        {
            foreach (var element in parent.Elements())
            {
                switch (element.Name.LocalName)
                {
                    case "PropertyGroup":
                        if (IsTrue(context, element, filePath))
                        {
                            foreach (var property in element.Elements())
                            {
                                if (IsTrue(context, property, filePath))
                                {
                                    context.Properties.Set(property.Name.LocalName, context.Properties.Expand(property.Value.Trim()));
                                }
                            }
                        }

                        break;
                    case "ItemGroup":
                        if (IsTrue(context, element, filePath))
                        {
                            CollectItems(context, element, filePath);
                        }

                        break;
                    case "Choose":
                        EvaluateChoose(context, filePath, element, depth);
                        break;
                    case "Import":
                        EvaluateImport(context, filePath, element, depth);
                        break;
                }
            }
        }

        private void EvaluateChoose(EvaluationContext context, string filePath, XElement choose, int depth)
        {
            foreach (var when in choose.Elements().Where(e => e.Name.LocalName == "When"))
            {
                if (IsTrue(context, when, filePath))
                {
                    EvaluateChildren(context, filePath, when, depth);
                    return;
                }
            }

            var otherwise = choose.Elements().FirstOrDefault(e => e.Name.LocalName == "Otherwise");
            if (otherwise != null)
            {
                EvaluateChildren(context, filePath, otherwise, depth);
            }
        }

        private void EvaluateImport(EvaluationContext context, string filePath, XElement import, int depth)
        {
            // Gli import di SDK e dei target di sistema non fanno parte delle informazioni del progetto.
            if (import.Attribute("Sdk") != null || depth >= MaxImportDepth || !IsTrue(context, import, filePath))
            {
                return;
            }

            bool unresolved;
            var projectAttribute = (string)import.Attribute("Project") ?? string.Empty;
            var target = context.Properties.Expand(projectAttribute, out unresolved).Trim();
            if (unresolved || target.Length == 0 || target.IndexOfAny(new[] { '*', '?' }) >= 0)
            {
                return;
            }

            var resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(filePath), NormalizeSeparators(target)));
            if (!File.Exists(resolved) || !context.VisitedImports.Add(resolved))
            {
                return;
            }

            try
            {
                EvaluateFile(context, resolved, LoadXml(resolved).Root, depth + 1);
            }
            catch (XmlException ex)
            {
                context.Info.Warnings.Add("Import ignorato (XML non valido): " + resolved + " - " + ex.Message);
            }
            finally
            {
                context.Properties.Set("MSBuildThisFileDirectory", Path.GetDirectoryName(filePath) + Path.DirectorySeparatorChar);
                context.Properties.Set("MSBuildThisFileFullPath", filePath);
            }
        }

        private static void CollectItems(EvaluationContext context, XElement itemGroup, string filePath)
        {
            foreach (var item in itemGroup.Elements())
            {
                if (item.Name.LocalName == "COMReference" && IsTrue(context, item, filePath))
                {
                    context.Info.HasComReferences = true;
                }
                else if (item.Name.LocalName == "Reference" && IsTrue(context, item, filePath))
                {
                    var hintPath = item.Elements().FirstOrDefault(e => e.Name.LocalName == "HintPath");
                    if (hintPath != null)
                    {
                        context.HintPaths.Add(context.Properties.Expand(hintPath.Value.Trim()));
                    }
                }
            }
        }

        private static bool IsTrue(EvaluationContext context, XElement element, string filePath)
        {
            var condition = (string)element.Attribute("Condition");
            if (string.IsNullOrWhiteSpace(condition))
            {
                return true;
            }

            try
            {
                return new ConditionEvaluator(context.Properties, context.Info.Directory).Evaluate(condition);
            }
            catch (ConditionException ex)
            {
                var lineInfo = (IXmlLineInfo)element;
                context.Info.Warnings.Add(string.Format(
                    "{0}({1}): condizione considerata falsa ({2}): {3}",
                    Path.GetFileName(filePath),
                    lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
                    ex.Message,
                    condition.Trim()));
                return false;
            }
        }

        private static void FillProjectInfo(ProjectInfo info, EvaluationContext context)
        {
            var p = context.Properties;

            info.OutputTypeText = p.Get("OutputType");
            info.OutputKind = ParseOutputKind(info.OutputTypeText);
            info.AssemblyName = p.IsDefined("AssemblyName") && p.Get("AssemblyName").Length > 0 ? p.Get("AssemblyName") : info.Name;
            info.UseWpf = IsTrueValue(p.Get("UseWPF"));
            info.UseWindowsForms = IsTrueValue(p.Get("UseWindowsForms"));
            info.RuntimeIdentifier = NullIfEmpty(p.Get("RuntimeIdentifier"));
            info.UsesPackagesConfig = !info.IsSdkStyle && File.Exists(Path.Combine(info.Directory, "packages.config"));

            var projectTypeGuids = p.Get("ProjectTypeGuids").ToLowerInvariant();
            info.IsWebProject =
                (info.Sdk != null && info.Sdk.StartsWith("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase))
                || WebProjectTypeGuids.Any(g => projectTypeGuids.Contains(g));

            if (info.Sdk != null && info.Sdk.Equals("Microsoft.NET.Sdk.WindowsDesktop", StringComparison.OrdinalIgnoreCase)
                && !info.UseWpf && !info.UseWindowsForms)
            {
                info.Warnings.Add("SDK WindowsDesktop senza UseWPF/UseWindowsForms.");
            }

            if (info.IsSdkStyle)
            {
                ReadSdkTargetFrameworks(info, p);
            }
            else
            {
                var version = p.Get("TargetFrameworkVersion");
                if (version.Length > 0)
                {
                    info.TargetFrameworks.Add(TargetFramework.FromIdentifier(p.Get("TargetFrameworkIdentifier"), version));
                }
                else
                {
                    info.Warnings.Add("TargetFrameworkVersion non trovato: MSBuild userebbe il proprio default.");
                }
            }
        }

        private static void ReadSdkTargetFrameworks(ProjectInfo info, PropertyBag p)
        {
            // In MSBuild, se TargetFrameworks (plurale) è valorizzato, prevale su TargetFramework.
            bool unresolved;
            var multi = p.Expand(p.Get("TargetFrameworks"), out unresolved);
            var raw = multi.Trim().Length > 0 ? multi : p.Expand(p.Get("TargetFramework"), out unresolved);

            foreach (var moniker in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(m => m.Trim()).Where(m => m.Length > 0))
            {
                if (moniker.Contains("$("))
                {
                    info.Warnings.Add("Target framework non risolvibile staticamente: " + moniker);
                    continue;
                }

                var tfm = TargetFramework.Parse(moniker);
                if (!info.TargetFrameworks.Contains(tfm))
                {
                    info.TargetFrameworks.Add(tfm);
                }
            }
        }

        private const int MaxSolutionSearchLevels = 6;

        /// <summary>
        /// Progetto scelto senza solution: cerca la cartella della solution come farebbe Visual Studio.
        /// 1) una .sln/.slnx nelle cartelle superiori che contiene il progetto;
        /// 2) altrimenti la cartella che contiene "packages\" negli HintPath dei riferimenti NuGet.
        /// </summary>
        private static void InferSolutionDir(ProjectInfo info, IEnumerable<string> hintPaths)
        {
            var solution = FindContainingSolution(info);
            if (solution != null)
            {
                info.InferredSolutionDir = Path.GetDirectoryName(solution);
                info.InferredSolutionDirSource = "solution " + Path.GetFileName(solution);
                return;
            }

            foreach (var hint in hintPaths)
            {
                var packagesDir = FindPackagesDirectory(info.Directory, hint);
                if (packagesDir != null)
                {
                    info.InferredSolutionDir = Path.GetDirectoryName(packagesDir);
                    info.InferredSolutionDirSource = "HintPath dei pacchetti NuGet";
                    return;
                }
            }
        }

        private static string FindContainingSolution(ProjectInfo info)
        {
            var parser = new SolutionParser();
            var directory = new DirectoryInfo(info.Directory);
            for (var level = 0; directory != null && level < MaxSolutionSearchLevels; level++, directory = directory.Parent)
            {
                string[] candidates;
                try
                {
                    candidates = directory.GetFiles("*.sln").Concat(directory.GetFiles("*.slnx"))
                        .Select(f => f.FullName)
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    continue;
                }

                foreach (var candidate in candidates)
                {
                    try
                    {
                        if (parser.Parse(candidate).Any(p => string.Equals(p.FullPath, info.FullPath, StringComparison.OrdinalIgnoreCase)))
                        {
                            return candidate;
                        }
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is XmlException)
                    {
                        // Solution illeggibile: si passa alla successiva.
                    }
                }
            }

            return null;
        }

        // "..\packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll" -> "<...>\packages"
        private static string FindPackagesDirectory(string projectDirectory, string hintPath)
        {
            if (string.IsNullOrWhiteSpace(hintPath) || hintPath.Contains("$("))
            {
                return null;
            }

            string full;
            try
            {
                var normalized = NormalizeSeparators(hintPath);
                full = Path.GetFullPath(Path.IsPathRooted(normalized) ? normalized : Path.Combine(projectDirectory, normalized));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                return null;
            }

            var directory = new DirectoryInfo(Path.GetDirectoryName(full));
            while (directory != null)
            {
                if (string.Equals(directory.Name, "packages", StringComparison.OrdinalIgnoreCase) && directory.Parent != null)
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static OutputKind ParseOutputKind(string outputType)
        {
            switch ((outputType ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "":
                case "library":
                case "module":
                case "winmdobj":
                    return OutputKind.Library;
                case "exe":
                    return OutputKind.Exe;
                case "winexe":
                    return OutputKind.WinExe;
                default:
                    return OutputKind.Other;
            }
        }

        private static string DetectSdk(XElement root)
        {
            var sdk = (string)root.Attribute("Sdk");
            if (!string.IsNullOrWhiteSpace(sdk))
            {
                return StripVersion(sdk.Split(';')[0]);
            }

            // <Sdk Name="..."/> oppure <Import Project="Sdk.props" Sdk="..."/>
            var sdkElement = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Sdk");
            if (sdkElement != null)
            {
                return StripVersion((string)sdkElement.Attribute("Name"));
            }

            var sdkImport = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Import" && e.Attribute("Sdk") != null);
            return sdkImport != null ? StripVersion((string)sdkImport.Attribute("Sdk")) : null;
        }

        private static string StripVersion(string sdk)
        {
            if (sdk == null)
            {
                return null;
            }

            var slash = sdk.IndexOf('/');
            return (slash > 0 ? sdk.Substring(0, slash) : sdk).Trim();
        }

        private static string FindFileAbove(string startDirectory, string fileName)
        {
            var directory = new DirectoryInfo(startDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }

        // MSBuild accetta sia '\' sia '/' nei percorsi.
        internal static string NormalizeSeparators(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        private static XDocument LoadXml(string path)
        {
            return XDocument.Load(path, LoadOptions.SetLineInfo);
        }

        private static bool IsTrueValue(string value)
        {
            return string.Equals(value.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private sealed class EvaluationContext
        {
            public EvaluationContext(ProjectInfo info)
            {
                Info = info;
                Properties = new PropertyBag();
                VisitedImports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HintPaths = new List<string>();
            }

            public List<string> HintPaths { get; private set; }

            public ProjectInfo Info { get; private set; }

            public PropertyBag Properties { get; private set; }

            public HashSet<string> VisitedImports { get; private set; }
        }
    }
}
