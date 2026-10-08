namespace SolutionDoctor.Core.Model
{
    /// <summary>Una classe candidata al refactoring, con il punteggio che ne decide la priorità.</summary>
    public sealed class BacklogItem
    {
        public string Project { get; set; }

        public string Subject { get; set; }

        public string FilePath { get; set; }

        /// <summary>Righe del code-behind; 0 se la classe non è una form.</summary>
        public int Lines { get; set; }

        public int Handlers { get; set; }

        public int FindingCount { get; set; }

        /// <summary>Punteggio dei soli problemi (somma dei pesi per gravità).</summary>
        public int Score { get; set; }

        /// <summary>Commit che hanno toccato il file negli ultimi mesi; 0 se git non è disponibile.</summary>
        public int Churn { get; set; }

        /// <summary>Score amplificato dalla frequenza di modifica: ordina il backlog.</summary>
        public double Priority { get; set; }

        /// <summary>Regole scattate con il numero di occorrenze, es. "WF004×2, WF007×3".</summary>
        public string Rules { get; set; }
    }
}
