using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Core.Reporting
{
    /// <summary>Rende l'esito dell'analisi in un documento Markdown (leggibile anche su GitHub, grafo Mermaid compreso).</summary>
    public static class MarkdownReportWriter
    {
        private const int BacklogSize = 15;
        private const int FindingsPerRule = 50;

        // Il report è un documento: deve risultare identico su ogni macchina, qualunque sia la lingua del sistema
        // (e funzionare anche con la globalizzazione invariante, senza ICU).
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        public static string Write(AnalysisReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# SolutionDoctor — " + report.Solution.Name);
            sb.AppendLine();
            sb.AppendLine("Generato il " + report.GeneratedAt.ToString("yyyy-MM-dd HH:mm", Culture)
                + " con SolutionDoctor " + report.ToolVersion + ".");
            sb.AppendLine();

            WriteSummary(sb, report);
            WriteBacklog(sb, report);
            WriteProjects(sb, report);
            WriteMigrationOrder(sb, report);
            WriteFindings(sb, report);
            WriteLegend(sb, report);
            WriteWarnings(sb, report);
            return sb.ToString();
        }

        private static void WriteSummary(StringBuilder sb, AnalysisReport report)
        {
            var projects = report.Solution.Projects;
            sb.AppendLine("## Riepilogo");
            sb.AppendLine();
            sb.AppendLine("- Progetti: **" + projects.Count + "** (WinForms: " + projects.Count(p => p.IsWinForms)
                + ", con packages.config: " + projects.Count(p => p.UsesPackagesConfig)
                + ", non SDK-style: " + projects.Count(p => !p.IsSdkStyle) + ")");
            sb.AppendLine("- Form e user control: **" + report.UiClasses.Count + "**");
            sb.AppendLine("- Problemi: **" + report.Findings.Count + "** — "
                + string.Join(", ", new[] { Severity.High, Severity.Medium, Severity.Low, Severity.Info }
                    .Select(s => report.Findings.Count(f => f.Severity == s) + " " + Label(s).ToLowerInvariant())));
            sb.AppendLine("- Cronologia git: " + (report.ChurnAvailable ? "usata per ordinare il backlog" : "non disponibile (priorità basata solo sui problemi)"));
            sb.AppendLine();
        }

        private static void WriteBacklog(StringBuilder sb, AnalysisReport report)
        {
            sb.AppendLine("## Da dove cominciare");
            sb.AppendLine();
            if (report.Backlog.Count == 0)
            {
                sb.AppendLine("Nessuna classe con problemi nel codice.");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("Classi ordinate per priorità = punteggio dei problemi × frequenza di modifica (commit recenti).");
            sb.AppendLine();
            sb.AppendLine("| # | Classe | Progetto | Righe | Handler | Problemi | Commit | Priorità | Regole |");
            sb.AppendLine("|--:|---|---|--:|--:|--:|--:|--:|---|");
            var rank = 1;
            foreach (var item in report.Backlog.Take(BacklogSize))
            {
                sb.AppendLine("| " + rank++ + " | " + Cell(item.Subject) + " | " + Cell(item.Project)
                    + " | " + (item.Lines > 0 ? item.Lines.ToString(Culture) : "—") + " | " + item.Handlers
                    + " | " + item.FindingCount + " | " + item.Churn
                    + " | " + item.Priority.ToString("0.#", Culture) + " | " + Cell(item.Rules) + " |");
            }

            if (report.Backlog.Count > BacklogSize)
            {
                sb.AppendLine();
                sb.AppendLine("…e altre " + (report.Backlog.Count - BacklogSize) + " classi.");
            }

            sb.AppendLine();
        }

        private static void WriteProjects(StringBuilder sb, AnalysisReport report)
        {
            sb.AppendLine("## Progetti");
            sb.AppendLine();
            sb.AppendLine("| Progetto | Framework | Formato | Pacchetti | Tipo | WinForms |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var p in report.Solution.Projects.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine("| " + Cell(p.Name) + " | " + Cell(p.TargetFrameworks.Count > 0 ? string.Join(", ", p.TargetFrameworks) : "?")
                    + " | " + (p.IsSdkStyle ? "SDK-style" : "classico")
                    + " | " + p.Packages.Count + (p.UsesPackagesConfig ? " (packages.config)" : string.Empty)
                    + " | " + Cell(p.OutputType) + " | " + (p.IsWinForms ? "sì" : "no") + " |");
            }

            sb.AppendLine();
        }

        private static void WriteMigrationOrder(StringBuilder sb, AnalysisReport report)
        {
            sb.AppendLine("## Ordine di migrazione consigliato");
            sb.AppendLine();
            sb.AppendLine("Dalle foglie verso gli eseguibili: un progetto si migra dopo i progetti che referenzia.");
            sb.AppendLine();
            var position = 1;
            foreach (var p in report.MigrationOrder)
            {
                sb.AppendLine(position++ + ". " + p.Name);
            }

            if (report.CycleMembers.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("> **Attenzione:** riferimenti circolari. Non ordinabili: "
                    + string.Join(", ", report.CycleMembers.Select(p => p.Name)) + ". Rompere il ciclo prima di migrare.");
            }

            WriteGraph(sb, report);
            sb.AppendLine();
        }

        private static void WriteGraph(StringBuilder sb, AnalysisReport report)
        {
            var projects = report.Solution.Projects;
            if (projects.Count < 2)
            {
                return;
            }

            var ids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < projects.Count; i++)
            {
                ids[projects[i].Path] = "P" + i;
            }

            sb.AppendLine();
            sb.AppendLine("```mermaid");
            sb.AppendLine("graph LR");
            foreach (var p in projects)
            {
                sb.AppendLine("  " + ids[p.Path] + "[\"" + p.Name.Replace("\"", "'") + "\"]");
            }

            foreach (var p in projects)
            {
                foreach (var reference in p.ProjectReferences.Where(ids.ContainsKey).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    sb.AppendLine("  " + ids[p.Path] + " --> " + ids[reference]);
                }
            }

            sb.AppendLine("```");
        }

        private static void WriteFindings(StringBuilder sb, AnalysisReport report)
        {
            sb.AppendLine("## Problemi per regola");
            sb.AppendLine();
            if (report.Findings.Count == 0)
            {
                sb.AppendLine("Nessun problema rilevato.");
                sb.AppendLine();
                return;
            }

            foreach (var group in report.Findings.GroupBy(f => f.RuleId).OrderBy(g => g.Key))
            {
                var rule = RuleCatalog.Find(group.Key);
                sb.AppendLine("### " + group.Key + " — " + (rule != null ? rule.Title : group.Key) + " (" + group.Count() + ")");
                sb.AppendLine();
                foreach (var f in group.Take(FindingsPerRule))
                {
                    sb.AppendLine("- **" + Label(f.Severity) + "** `" + f.FilePath + ":" + f.Line + "` — " + f.Message);
                }

                if (group.Count() > FindingsPerRule)
                {
                    sb.AppendLine("- …e altri " + (group.Count() - FindingsPerRule) + ".");
                }

                sb.AppendLine();
            }
        }

        private static void WriteLegend(StringBuilder sb, AnalysisReport report)
        {
            var used = new HashSet<string>(report.Findings.Select(f => f.RuleId));
            if (used.Count == 0)
            {
                return;
            }

            sb.AppendLine("## Come intervenire");
            sb.AppendLine();
            foreach (var rule in RuleCatalog.All.Where(r => used.Contains(r.Id)))
            {
                sb.AppendLine("- **" + rule.Id + "** " + rule.Title + ". " + rule.Advice);
            }

            sb.AppendLine();
        }

        private static void WriteWarnings(StringBuilder sb, AnalysisReport report)
        {
            if (report.Solution.Warnings.Count == 0)
            {
                return;
            }

            sb.AppendLine("## Avvisi");
            sb.AppendLine();
            foreach (var warning in report.Solution.Warnings)
            {
                sb.AppendLine("- " + warning);
            }

            sb.AppendLine();
        }

        public static string Label(Severity severity)
        {
            switch (severity)
            {
                case Severity.High: return "Alta";
                case Severity.Medium: return "Media";
                case Severity.Low: return "Bassa";
                default: return "Info";
            }
        }

        private static string Cell(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|");
        }
    }
}
