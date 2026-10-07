using System.Collections.Generic;
using BuildExe.Core.Execution;

namespace BuildExe.Core.Model
{
    public enum BuildTool
    {
        None,
        DotNetCli,
        MsBuild
    }

    /// <summary>
    /// Decisione presa dal planner: quale strumento usare, con quali argomenti, e perché.
    /// </summary>
    public sealed class BuildPlan
    {
        public BuildPlan()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
            Notes = new List<string>();
            Arguments = new List<string>();
        }

        public BuildTool Tool { get; set; }

        public string Executable { get; set; }

        public List<string> Arguments { get; private set; }

        public string WorkingDirectory { get; set; }

        public TargetFramework TargetFramework { get; set; }

        /// <summary>Spiegazione leggibile della scelta (mostrata nella UI).</summary>
        public string Description { get; set; }

        public List<string> Errors { get; private set; }

        public List<string> Warnings { get; private set; }

        /// <summary>Informazioni sulle scelte fatte (non sono problemi).</summary>
        public List<string> Notes { get; private set; }

        public bool CanBuild
        {
            get { return Errors.Count == 0 && Tool != BuildTool.None; }
        }

        public string ArgumentsText
        {
            get { return CommandLine.Join(Arguments); }
        }

        public string CommandLineText
        {
            get { return Executable == null ? string.Empty : CommandLine.Quote(Executable) + " " + ArgumentsText; }
        }
    }
}
