using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BuildExe.Core.Model
{
    public enum OutputKind
    {
        Library,
        Exe,
        WinExe,
        Other
    }

    /// <summary>
    /// Risultato dell'analisi statica di un file di progetto (.csproj/.vbproj/.fsproj).
    /// </summary>
    public sealed class ProjectInfo
    {
        public ProjectInfo(string path)
        {
            FullPath = System.IO.Path.GetFullPath(path);
            TargetFrameworks = new List<TargetFramework>();
            Warnings = new List<string>();
        }

        public string FullPath { get; private set; }

        public string Name
        {
            get { return System.IO.Path.GetFileNameWithoutExtension(FullPath); }
        }

        public string Directory
        {
            get { return System.IO.Path.GetDirectoryName(FullPath); }
        }

        public string Language
        {
            get
            {
                switch (System.IO.Path.GetExtension(FullPath).ToLowerInvariant())
                {
                    case ".vbproj":
                        return "Visual Basic";
                    case ".fsproj":
                        return "F#";
                    default:
                        return "C#";
                }
            }
        }

        /// <summary>True per i progetti "SDK-style" (&lt;Project Sdk="..."&gt;).</summary>
        public bool IsSdkStyle { get; set; }

        /// <summary>Nome dell'SDK (Microsoft.NET.Sdk, Microsoft.NET.Sdk.WindowsDesktop, ...).</summary>
        public string Sdk { get; set; }

        public OutputKind OutputKind { get; set; }

        public string OutputTypeText { get; set; }

        public string AssemblyName { get; set; }

        public List<TargetFramework> TargetFrameworks { get; private set; }

        public bool UseWpf { get; set; }

        public bool UseWindowsForms { get; set; }

        public bool IsWebProject { get; set; }

        /// <summary>Progetto classico con NuGet in formato packages.config.</summary>
        public bool UsesPackagesConfig { get; set; }

        /// <summary>COMReference: non supportati dalla CLI dotnet, richiedono MSBuild.exe.</summary>
        public bool HasComReferences { get; set; }

        public string RuntimeIdentifier { get; set; }

        /// <summary>
        /// Cartella della solution dedotta quando il progetto viene scelto da solo (senza .sln):
        /// serve a valorizzare $(SolutionDir), indispensabile al restore NuGet con packages.config.
        /// </summary>
        public string InferredSolutionDir { get; set; }

        /// <summary>Da dove è stata dedotta <see cref="InferredSolutionDir"/> (per la UI).</summary>
        public string InferredSolutionDirSource { get; set; }

        /// <summary>File Directory.Build.props importato, se presente.</summary>
        public string DirectoryBuildProps { get; set; }

        /// <summary>Avvisi raccolti durante l'analisi (condizioni non valutabili, valori non risolti, ...).</summary>
        public List<string> Warnings { get; private set; }

        public bool IsExecutable
        {
            get { return OutputKind == OutputKind.Exe || OutputKind == OutputKind.WinExe; }
        }

        /// <summary>
        /// Target proposto di default: il .NET più recente, altrimenti il .NET Framework più recente.
        /// </summary>
        public TargetFramework DefaultTargetFramework
        {
            get
            {
                return TargetFrameworks
                    .Where(t => t.CanProduceExecutable)
                    .OrderByDescending(t => t.Family == FrameworkFamily.NetCore)
                    .ThenByDescending(t => t.Version)
                    .FirstOrDefault()
                    ?? TargetFrameworks.FirstOrDefault();
            }
        }

        public string ProjectStyleDescription
        {
            get { return IsSdkStyle ? "SDK-style (" + (Sdk ?? "Microsoft.NET.Sdk") + ")" : "Classico (non SDK-style)"; }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
