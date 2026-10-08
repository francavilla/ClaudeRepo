namespace SolutionDoctor.Core.Model
{
    /// <summary>Un problema rilevato dall'analisi, con la posizione nel codice.</summary>
    public sealed class Finding
    {
        public Finding(string ruleId, Severity severity, string message, string filePath, int line, string subject)
        {
            RuleId = ruleId;
            Severity = severity;
            Message = message;
            FilePath = filePath;
            Line = line;
            Subject = subject;
        }

        public string RuleId { get; private set; }

        public Severity Severity { get; private set; }

        public string Message { get; private set; }

        /// <summary>Percorso relativo alla cartella della solution, con '/' come separatore.</summary>
        public string FilePath { get; private set; }

        public int Line { get; private set; }

        /// <summary>Classe (codice) o progetto (inventario) a cui il problema si riferisce.</summary>
        public string Subject { get; private set; }

        /// <summary>Nome del progetto che contiene il problema.</summary>
        public string Project { get; set; }
    }
}
