using System;
using System.IO;
using System.Linq;
using BuildExe.Core.Model;

namespace BuildExe.Core.Planning
{
    /// <summary>
    /// Decide come compilare il progetto, a partire SOLO dalle informazioni del progetto
    /// e dagli strumenti installati:
    /// <list type="bullet">
    /// <item>SDK-style (net8.0, net48, netcoreapp3.1, ...) → <c>dotnet publish</c> con un SDK di versione adeguata;</item>
    /// <item>classico .NET Framework (TargetFrameworkVersion v4.x) → <c>MSBuild.exe</c> di Visual Studio/Build Tools.</item>
    /// </list>
    /// Classe pura (nessun I/O): interamente testabile.
    /// </summary>
    public sealed class BuildPlanner
    {
        private const string DotNetDownloadUrl = "https://dotnet.microsoft.com/download/dotnet/";
        private const string DeveloperPackUrl = "https://dotnet.microsoft.com/download/visual-studio-sdks";

        // MSBuild 15.5: switch -restore. MSBuild 16.5: RestorePackagesConfig.
        private static readonly Version MinMsBuildForRestore = new Version(15, 5);
        private static readonly Version MinMsBuildForPackagesConfig = new Version(16, 5);

        public BuildPlan CreatePlan(ProjectInfo project, BuildOptions options, Toolchains toolchains)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (toolchains == null)
            {
                throw new ArgumentNullException("toolchains");
            }

            var plan = new BuildPlan { WorkingDirectory = project.Directory };
            plan.Warnings.AddRange(project.Warnings);

            if (string.IsNullOrWhiteSpace(options.OutputDirectory))
            {
                plan.Errors.Add("Indicare la cartella di output.");
            }

            if (project.IsWebProject)
            {
                plan.Errors.Add("È un progetto web: non produce un eseguibile.");
            }
            else if (!project.IsExecutable)
            {
                plan.Errors.Add(string.Format(
                    "OutputType = '{0}': il progetto non produce un eseguibile (servono Exe o WinExe).",
                    string.IsNullOrEmpty(project.OutputTypeText) ? "Library (default)" : project.OutputTypeText));
            }

            var tfm = options.TargetFramework ?? project.DefaultTargetFramework;
            if (tfm == null)
            {
                plan.Errors.Add("Impossibile determinare il target framework dal progetto.");
                return plan;
            }

            if (!project.TargetFrameworks.Contains(tfm))
            {
                plan.Errors.Add("Il target " + tfm.Moniker + " non è tra quelli del progetto.");
                return plan;
            }

            plan.TargetFramework = tfm;
            if (!tfm.CanProduceExecutable)
            {
                plan.Errors.Add(tfm.DisplayName + " non può produrre un eseguibile.");
                return plan;
            }

            if (project.IsSdkStyle)
            {
                PlanSdkStyle(plan, project, options, toolchains, tfm);
            }
            else
            {
                PlanLegacy(plan, project, options, toolchains, tfm);
            }

            return plan;
        }

        private static void PlanSdkStyle(BuildPlan plan, ProjectInfo project, BuildOptions options, Toolchains toolchains, TargetFramework tfm)
        {
            // I COMReference non sono supportati dalla CLI dotnet (MSBuild per .NET Core): serve MSBuild.exe.
            if (project.HasComReferences && toolchains.MsBuildPath != null && toolchains.MsBuildVersion >= new Version(16, 0))
            {
                PlanSdkStyleWithMsBuild(plan, project, options, toolchains, tfm);
                return;
            }

            if (project.HasComReferences)
            {
                plan.Warnings.Add("Il progetto contiene COMReference: con la CLI dotnet la build potrebbe fallire. Installare Visual Studio/Build Tools.");
            }

            var requiredMajor = RequiredSdkMajor(project, tfm);
            if (toolchains.DotNetPath == null || toolchains.DotNetSdks.Count == 0)
            {
                plan.Errors.Add(string.Format(
                    "Nessun .NET SDK installato. Per {0} serve .NET SDK {1}.0 o superiore: {2}{1}.0",
                    tfm.DisplayName, requiredMajor, DotNetDownloadUrl));
                return;
            }

            var sdk = toolchains.BestSdk(requiredMajor);
            if (sdk == null)
            {
                plan.Errors.Add(string.Format(
                    "Per {0} serve .NET SDK {1}.0 o superiore; installati: {2}. Download: {3}{1}.0",
                    tfm.DisplayName, requiredMajor, string.Join(", ", toolchains.DotNetSdks), DotNetDownloadUrl));
                return;
            }

            // Da .NET 5 SDK le reference assemblies .NET Framework arrivano da NuGet se il Developer Pack manca.
            if (tfm.Family == FrameworkFamily.NetFramework && sdk.Major < 5 && !toolchains.HasTargetingPack(tfm.Version))
            {
                plan.Warnings.Add("Targeting pack " + tfm.DisplayName + " non installato: " + DeveloperPackUrl);
            }

            plan.Tool = BuildTool.DotNetCli;
            plan.Executable = toolchains.DotNetPath;
            plan.Arguments.AddRange(new[]
            {
                "publish", project.FullPath,
                "-c", options.Configuration,
                "-f", tfm.Moniker,
                "-o", options.OutputDirectory ?? string.Empty,
                "--nologo"
            });

            var description = string.Format(
                "Progetto {0} per {1} → dotnet publish (SDK {2} o successivo, richiesto ≥ {3}.0)",
                project.ProjectStyleDescription, tfm.DisplayName, sdk, requiredMajor);

            if (tfm.Family == FrameworkFamily.NetCore)
            {
                description += AddNetCoreDeploymentOptions(plan, options);
            }
            else if (options.SelfContained || options.SingleFile)
            {
                plan.Warnings.Add("Self-contained e single-file sono disponibili solo per .NET Core/.NET 5+: opzioni ignorate.");
            }

            AddSolutionDir(plan, project, options);
            plan.Description = description;
        }

        private static string AddNetCoreDeploymentOptions(BuildPlan plan, BuildOptions options)
        {
            if (!options.SelfContained && !options.SingleFile)
            {
                return string.Empty;
            }

            var rid = string.IsNullOrWhiteSpace(options.RuntimeIdentifier) ? "win-x64" : options.RuntimeIdentifier.Trim();
            plan.Arguments.AddRange(new[] { "-r", rid, "--self-contained", options.SelfContained ? "true" : "false" });
            if (options.SingleFile)
            {
                plan.Arguments.Add("-p:PublishSingleFile=true");
            }

            return string.Format(", {0}{1}, RID {2}", options.SelfContained ? "self-contained" : "framework-dependent", options.SingleFile ? ", file singolo" : string.Empty, rid);
        }

        private static void PlanSdkStyleWithMsBuild(BuildPlan plan, ProjectInfo project, BuildOptions options, Toolchains toolchains, TargetFramework tfm)
        {
            plan.Tool = BuildTool.MsBuild;
            plan.Executable = toolchains.MsBuildPath;
            plan.Arguments.AddRange(new[]
            {
                project.FullPath,
                "-restore",
                "-t:Publish",
                "-p:Configuration=" + options.Configuration,
                "-p:TargetFramework=" + tfm.Moniker,
                "-p:PublishDir=" + WithTrailingSlash(options.OutputDirectory),
                "-m",
                "-nologo",
                "-v:minimal"
            });
            AddSolutionDir(plan, project, options);
            plan.Description = string.Format(
                "Progetto {0} per {1} con COMReference → MSBuild {2} -t:Publish",
                project.ProjectStyleDescription, tfm.DisplayName, toolchains.MsBuildVersion);
        }

        private static void PlanLegacy(BuildPlan plan, ProjectInfo project, BuildOptions options, Toolchains toolchains, TargetFramework tfm)
        {
            if (tfm.Family != FrameworkFamily.NetFramework)
            {
                plan.Errors.Add("Progetto classico con target " + tfm.DisplayName + " non supportato.");
                return;
            }

            if (options.SelfContained || options.SingleFile)
            {
                plan.Warnings.Add("Self-contained e single-file non si applicano a .NET Framework: opzioni ignorate.");
            }

            if (toolchains.MsBuildPath == null)
            {
                plan.Errors.Add("MSBuild non trovato. Installare Visual Studio o \"Build Tools for Visual Studio\" (carico di lavoro .NET desktop).");
                return;
            }

            if (toolchains.MsBuildVersion < MinMsBuildForRestore)
            {
                plan.Errors.Add(string.Format(
                    "Trovato solo MSBuild {0} del .NET Framework (C# 5, nessun restore NuGet). Installare Visual Studio 2017+ o Build Tools.",
                    toolchains.MsBuildVersion));
                return;
            }

            if (!toolchains.HasTargetingPack(tfm.Version))
            {
                plan.Errors.Add(string.Format(
                    "Targeting pack {0} non installato (errore MSB3644). Installare il Developer Pack: {1}",
                    tfm.DisplayName, DeveloperPackUrl));
                return;
            }

            plan.Tool = BuildTool.MsBuild;
            plan.Executable = toolchains.MsBuildPath;
            plan.Arguments.AddRange(new[]
            {
                project.FullPath,
                "-restore",
                "-t:Build",
                "-p:Configuration=" + options.Configuration,
                "-p:OutDir=" + WithTrailingSlash(options.OutputDirectory),
                "-m",
                "-nologo",
                "-v:minimal"
            });

            if (project.UsesPackagesConfig)
            {
                if (toolchains.MsBuildVersion >= MinMsBuildForPackagesConfig)
                {
                    plan.Arguments.Add("-p:RestorePackagesConfig=true");
                }
                else
                {
                    plan.Warnings.Add("packages.config richiede MSBuild 16.5+ per il restore: eseguire prima 'nuget restore'.");
                }
            }

            AddSolutionDir(plan, project, options);
            plan.Description = string.Format(
                "Progetto {0} per {1} → MSBuild {2}",
                project.ProjectStyleDescription, tfm.DisplayName, toolchains.MsBuildVersion);
        }

        /// <summary>
        /// Versione minima dell'SDK: la major del target per .NET (Core);
        /// 3 per WPF/WinForms (SDK WindowsDesktop); altrimenti 2.
        /// </summary>
        internal static int RequiredSdkMajor(ProjectInfo project, TargetFramework tfm)
        {
            var desktop = project.UseWpf || project.UseWindowsForms
                || string.Equals(project.Sdk, "Microsoft.NET.Sdk.WindowsDesktop", StringComparison.OrdinalIgnoreCase);
            var minimum = desktop ? 3 : 2;

            if (tfm.Family == FrameworkFamily.NetCore)
            {
                minimum = Math.Max(minimum, tfm.Version.Major);
            }

            // I target con versione di piattaforma Windows (net5.0-windows10.0.x) richiedono almeno .NET 5 SDK.
            if (!string.IsNullOrEmpty(tfm.Platform))
            {
                minimum = Math.Max(minimum, 5);
            }

            return minimum;
        }

        /// <summary>
        /// Valorizza $(SolutionDir) come farebbe Visual Studio: dalla solution scelta oppure da quella
        /// dedotta dall'analisi. Senza, il restore di packages.config fallisce ("Non è stata trovata alcuna soluzione").
        /// </summary>
        private static void AddSolutionDir(BuildPlan plan, ProjectInfo project, BuildOptions options)
        {
            string solutionDir = null;
            if (!string.IsNullOrEmpty(options.SolutionPath))
            {
                solutionDir = Path.GetDirectoryName(Path.GetFullPath(options.SolutionPath));
            }
            else if (!string.IsNullOrEmpty(project.InferredSolutionDir))
            {
                solutionDir = project.InferredSolutionDir;
                plan.Notes.Add("SolutionDir = " + solutionDir + " (da " + project.InferredSolutionDirSource + ")");
            }

            if (solutionDir != null)
            {
                plan.Arguments.Add("-p:SolutionDir=" + WithTrailingSlash(solutionDir));
            }
            else if (project.UsesPackagesConfig)
            {
                plan.Warnings.Add("Progetto con packages.config senza solution: il restore NuGet potrebbe fallire. Selezionare la .sln che contiene il progetto.");
            }
        }

        private static string WithTrailingSlash(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return path.EndsWith("\\", StringComparison.Ordinal) || path.EndsWith("/", StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }
    }
}
