using System;
using System.Collections.Generic;
using System.Linq;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Core.Analysis
{
    /// <summary>
    /// Costruisce il backlog di refactoring: una riga per classe, ordinata per priorità.
    /// Priorità = punteggio dei problemi × (1 + commit recenti / 10, con tetto a 50 commit):
    /// a parità di problemi vince il codice che si modifica più spesso, dove il refactoring rende di più.
    /// </summary>
    internal static class BacklogBuilder
    {
        private const int ChurnCap = 50;

        public static List<BacklogItem> Build(IEnumerable<Finding> findings, IEnumerable<UiClassInfo> uiClasses, IDictionary<string, int> churn)
        {
            var classes = uiClasses.ToDictionary(c => c.Project + "|" + c.FullName, c => c);
            var items = new List<BacklogItem>();

            var groups = findings
                .Where(f => f.Subject != null && f.RuleId.StartsWith("WF", StringComparison.Ordinal))
                .GroupBy(f => f.Project + "|" + f.Subject);

            foreach (var group in groups)
            {
                UiClassInfo ui;
                classes.TryGetValue(group.Key, out ui);
                var first = group.First();
                var path = ui != null ? ui.FilePath : first.FilePath;
                var commits = churn == null ? 0 : ChurnOf(churn, path);
                var score = group.Sum(f => WeightOf(f.Severity));

                items.Add(new BacklogItem
                {
                    Project = first.Project,
                    Subject = first.Subject,
                    FilePath = path,
                    Lines = ui != null ? ui.Lines : 0,
                    Handlers = ui != null ? ui.Handlers : 0,
                    FindingCount = group.Count(),
                    Score = score,
                    Churn = commits,
                    Priority = Math.Round(score * (1 + Math.Min(commits, ChurnCap) / 10.0), 1),
                    Rules = string.Join(", ", group.GroupBy(f => f.RuleId).OrderBy(g => g.Key).Select(g => g.Key + "×" + g.Count()))
                });
            }

            return items
                .OrderByDescending(i => i.Priority)
                .ThenByDescending(i => i.Score)
                .ThenBy(i => i.Subject, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static int WeightOf(Severity severity)
        {
            switch (severity)
            {
                case Severity.High: return 10;
                case Severity.Medium: return 4;
                case Severity.Low: return 2;
                default: return 1;
            }
        }

        // Il file della form e il suo Designer cambiano insieme: si sommano.
        private static int ChurnOf(IDictionary<string, int> churn, string path)
        {
            var total = 0;
            int value;
            if (churn.TryGetValue(path, out value))
            {
                total += value;
            }

            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                && churn.TryGetValue(path.Substring(0, path.Length - 3) + ".Designer.cs", out value))
            {
                total += value;
            }

            return total;
        }
    }
}
