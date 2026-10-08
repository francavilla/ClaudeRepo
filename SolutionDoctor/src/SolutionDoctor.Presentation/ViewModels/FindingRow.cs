using System.IO;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Model;
using SolutionDoctor.Core.Reporting;

namespace SolutionDoctor.Presentation.ViewModels
{
    /// <summary>Un problema, pronto per essere mostrato in una griglia o in un pannello di dettaglio.</summary>
    public sealed class FindingRow
    {
        public FindingRow(Finding finding, string rootDirectory)
        {
            var rule = RuleCatalog.Find(finding.RuleId);
            RuleId = finding.RuleId;
            RuleTitle = rule != null ? rule.Title : finding.RuleId;
            Advice = rule != null ? rule.Advice : string.Empty;
            Severity = finding.Severity;
            SeverityLabel = MarkdownReportWriter.Label(finding.Severity);
            Message = finding.Message;
            Project = finding.Project ?? string.Empty;
            Subject = finding.Subject ?? string.Empty;
            FilePath = finding.FilePath;
            Line = finding.Line;
            Location = finding.FilePath + ":" + finding.Line;
            FullPath = Path.GetFullPath(Path.Combine(rootDirectory, finding.FilePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        public string RuleId { get; private set; }

        public string RuleTitle { get; private set; }

        /// <summary>Come intervenire.</summary>
        public string Advice { get; private set; }

        public Severity Severity { get; private set; }

        public string SeverityLabel { get; private set; }

        public string Message { get; private set; }

        public string Project { get; private set; }

        public string Subject { get; private set; }

        /// <summary>Percorso relativo alla cartella della solution, con '/'.</summary>
        public string FilePath { get; private set; }

        public int Line { get; private set; }

        /// <summary>"percorso:riga".</summary>
        public string Location { get; private set; }

        public string FullPath { get; private set; }
    }
}
