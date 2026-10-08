using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using SolutionDoctor.Core.Inventory;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Core.Analysis
{
    /// <summary>Punto d'ingresso dell'analisi: inventario, regole, grafo, priorità.</summary>
    public sealed class SolutionAnalyzer
    {
        public static string ToolVersion
        {
            get
            {
                var attribute = typeof(SolutionAnalyzer).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var version = attribute != null ? attribute.InformationalVersion : "0.0.0";
                var plus = version.IndexOf('+');
                return plus >= 0 ? version.Substring(0, plus) : version;
            }
        }

        public AnalysisReport Analyze(string path, AnalysisOptions options)
        {
            return Analyze(path, options, null, CancellationToken.None);
        }

        /// <summary>
        /// Come <see cref="Analyze(string, AnalysisOptions)"/>, con avanzamento (messaggi in italiano, anche da un thread
        /// secondario) e annullamento: alla richiesta solleva <see cref="OperationCanceledException"/> tra un file e l'altro.
        /// </summary>
        public AnalysisReport Analyze(string path, AnalysisOptions options, IProgress<string> progress, CancellationToken cancellationToken)
        {
            options = options ?? new AnalysisOptions();
            Report(progress, "Lettura di solution e progetti…");
            var solution = SolutionLoader.Load(path);

            var findings = new List<Finding>();
            var uiClasses = new List<UiClassInfo>();
            var index = 0;
            foreach (var project in solution.Projects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                index++;
                Report(progress, "Analisi del progetto " + project.Name + " (" + index + " di " + solution.Projects.Count + ")…");
                findings.AddRange(ProjectRules.Evaluate(project));
                if (project.IsWinForms)
                {
                    CodeScanner.Scan(project, solution.RootDirectory, findings, uiClasses, cancellationToken);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            var graph = DependencyGraph.Build(solution.Projects);

            IDictionary<string, int> churn = null;
            if (options.UseGit && options.ChurnProvider != null)
            {
                Report(progress, "Lettura della cronologia git…");
                churn = options.ChurnProvider.TryGetChurn(solution.RootDirectory, options.GitMonths);
            }

            cancellationToken.ThrowIfCancellationRequested();
            Report(progress, "Calcolo delle priorità…");
            var report = new AnalysisReport
            {
                ToolVersion = ToolVersion,
                GeneratedAt = options.Now ?? DateTime.Now,
                Solution = solution,
                ChurnAvailable = churn != null
            };

            report.Findings.AddRange(findings
                .OrderByDescending(f => f.Severity)
                .ThenBy(f => f.RuleId)
                .ThenBy(f => f.FilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.Line));
            report.UiClasses.AddRange(uiClasses.OrderByDescending(c => c.Lines));
            report.Backlog.AddRange(BacklogBuilder.Build(findings, uiClasses, churn));
            report.MigrationOrder.AddRange(graph.MigrationOrder);
            report.CycleMembers.AddRange(graph.CycleMembers);
            return report;
        }

        private static void Report(IProgress<string> progress, string message)
        {
            if (progress != null)
            {
                progress.Report(message);
            }
        }
    }
}
