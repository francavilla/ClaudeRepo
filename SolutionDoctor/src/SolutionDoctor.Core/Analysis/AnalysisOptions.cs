using System;
using SolutionDoctor.Core.Git;

namespace SolutionDoctor.Core.Analysis
{
    public sealed class AnalysisOptions
    {
        /// <summary>Usa la cronologia git per individuare i file modificati più spesso.</summary>
        public bool UseGit { get; set; } = true;

        public int GitMonths { get; set; } = 12;

        /// <summary>Sostituibile nei test; di default usa il comando git installato.</summary>
        public IChurnProvider ChurnProvider { get; set; } = new GitChurnProvider();

        /// <summary>Data riportata nel report; di default l'ora corrente.</summary>
        public DateTime? Now { get; set; }
    }
}
