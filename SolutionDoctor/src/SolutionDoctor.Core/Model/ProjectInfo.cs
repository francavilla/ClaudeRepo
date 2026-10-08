using System.Collections.Generic;

namespace SolutionDoctor.Core.Model
{
    /// <summary>Fotografia di un progetto C#, ricavata dal solo file di progetto.</summary>
    public sealed class ProjectInfo
    {
        public string Name { get; set; }

        /// <summary>Percorso completo del file .csproj.</summary>
        public string Path { get; set; }

        /// <summary>Percorso del .csproj relativo alla cartella della solution, con '/'.</summary>
        public string RelativePath { get; set; }

        public string Directory { get; set; }

        /// <summary>True per i .csproj nel formato SDK-style (<c>&lt;Project Sdk="..."&gt;</c>).</summary>
        public bool IsSdkStyle { get; set; }

        /// <summary>Moniker dei framework di destinazione (net48, net8.0-windows, ...).</summary>
        public List<string> TargetFrameworks { get; } = new List<string>();

        public string OutputType { get; set; }

        public bool IsWinForms { get; set; }

        /// <summary>True se accanto al progetto esiste un packages.config.</summary>
        public bool UsesPackagesConfig { get; set; }

        public List<PackageRef> Packages { get; } = new List<PackageRef>();

        /// <summary>Assembly referenziati (<c>&lt;Reference Include="..."&gt;</c>), senza versione.</summary>
        public List<string> AssemblyReferences { get; } = new List<string>();

        public int ComReferenceCount { get; set; }

        /// <summary>Percorsi completi dei progetti referenziati.</summary>
        public List<string> ProjectReferences { get; } = new List<string>();
    }
}
