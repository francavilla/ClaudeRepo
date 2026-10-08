using System;
using System.Collections.Generic;

namespace SolutionDoctor.Core.Model
{
    /// <summary>Risultato completo di un'analisi, pronto per essere reso in un report.</summary>
    public sealed class AnalysisReport
    {
        public string ToolVersion { get; set; }

        public DateTime GeneratedAt { get; set; }

        public SolutionInfo Solution { get; set; }

        /// <summary>Problemi ordinati per gravità decrescente.</summary>
        public List<Finding> Findings { get; } = new List<Finding>();

        public List<UiClassInfo> UiClasses { get; } = new List<UiClassInfo>();

        public List<BacklogItem> Backlog { get; } = new List<BacklogItem>();

        /// <summary>Progetti in ordine di migrazione consigliato: prima le dipendenze, poi chi le usa.</summary>
        public List<ProjectInfo> MigrationOrder { get; } = new List<ProjectInfo>();

        /// <summary>Progetti bloccati da un ciclo di riferimenti (o che ne dipendono).</summary>
        public List<ProjectInfo> CycleMembers { get; } = new List<ProjectInfo>();

        public bool ChurnAvailable { get; set; }
    }
}
