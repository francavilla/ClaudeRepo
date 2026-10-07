using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildExe.Core.Model
{
    /// <summary>
    /// Strumenti di build disponibili sulla macchina. Costruito da <see cref="Toolchain.ToolchainLocator"/>;
    /// nei test viene creato a mano.
    /// </summary>
    public sealed class Toolchains
    {
        public Toolchains()
        {
            DotNetSdks = new List<Version>();
            InstalledTargetingPacks = new List<Version>();
        }

        /// <summary>Percorso di dotnet.exe, oppure null se la CLI .NET non è installata.</summary>
        public string DotNetPath { get; set; }

        /// <summary>Versioni degli SDK restituite da "dotnet --list-sdks".</summary>
        public List<Version> DotNetSdks { get; private set; }

        /// <summary>MSBuild.exe di Visual Studio / Build Tools (trovato con vswhere) o, in mancanza, quello del .NET Framework.</summary>
        public string MsBuildPath { get; set; }

        public Version MsBuildVersion { get; set; }

        /// <summary>Targeting pack .NET Framework presenti in "Reference Assemblies" (es. 4.6.2, 4.8).</summary>
        public List<Version> InstalledTargetingPacks { get; private set; }

        public bool HasTargetingPack(Version version)
        {
            return InstalledTargetingPacks.Any(v => v.Major == version.Major && v.Minor == version.Minor && Math.Max(v.Build, 0) == Math.Max(version.Build, 0));
        }

        public Version BestSdk(int minimumMajor)
        {
            return DotNetSdks.Where(v => v.Major >= minimumMajor).OrderByDescending(v => v).FirstOrDefault();
        }
    }
}
