using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            options = options ?? new AnalysisOptions();
            var solution = SolutionLoader.Load(path);

            var findings = new List<Finding>();
            var uiClasses = new List<UiClassInfo>();
            foreach (var project in solution.Projects)
            {
                findings.AddRange(ProjectRules.Evaluate(project));
                if (project.IsWinForms)
                {
                    CodeScanner.Scan(project, solution.RootDirectory, findings, uiClasses);
                }
            }

            var graph = DependencyGraph.Build(solution.Projects);

            IDictionary<string, int> churn = null;
            if (options.UseGit && options.ChurnProvider != null)
            {
                churn = options.ChurnProvider.TryGetChurn(solution.RootDirectory, options.GitMonths);
            }

            var report = new AnalysisReport
            {
                ToolVersion = ToolVersion,
                GeneratedAt = options.Now ?? System.DateTime.Now,
                Solution = solution,
                ChurnAvailable = churn != null
            };

            report.Findings.AddRange(findings
                .OrderByDescending(f => f.Severity)
                .ThenBy(f => f.RuleId)
                .ThenBy(f => f.FilePath, System.StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.Line));
            report.UiClasses.AddRange(uiClasses.OrderByDescending(c => c.Lines));
            report.Backlog.AddRange(BacklogBuilder.Build(findings, uiClasses, churn));
            report.MigrationOrder.AddRange(graph.MigrationOrder);
            report.CycleMembers.AddRange(graph.CycleMembers);
            return report;
        }
    }
}
