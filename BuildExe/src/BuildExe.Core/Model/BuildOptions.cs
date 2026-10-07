namespace BuildExe.Core.Model
{
    public sealed class BuildOptions
    {
        public BuildOptions()
        {
            Configuration = "Release";
            RuntimeIdentifier = "win-x64";
        }

        /// <summary>Cartella di output assoluta.</summary>
        public string OutputDirectory { get; set; }

        public string Configuration { get; set; }

        /// <summary>Target scelto (per i progetti multi-target); null = default del progetto.</summary>
        public TargetFramework TargetFramework { get; set; }

        /// <summary>Solution di provenienza, se il progetto è stato scelto da una .sln.</summary>
        public string SolutionPath { get; set; }

        // Opzioni valide solo per .NET (Core) / .NET 5+.
        public bool SelfContained { get; set; }

        public bool SingleFile { get; set; }

        public string RuntimeIdentifier { get; set; }
    }
}
