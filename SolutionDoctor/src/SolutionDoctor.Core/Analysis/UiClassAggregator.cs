using System.Collections.Generic;
using System.Linq;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Core.Analysis
{
    /// <summary>
    /// Riunisce le parti partial di ogni form/user control, calcola la dimensione del code-behind
    /// e segnala le classi troppo grandi.
    /// </summary>
    internal static class UiClassAggregator
    {
        private const int LargeLines = 400;
        private const int VeryLargeLines = 800;
        private const int HugeLines = 1500;

        public static List<UiClassInfo> Aggregate(string project, IEnumerable<ClassPart> parts, List<Finding> findings)
        {
            var result = new List<UiClassInfo>();

            foreach (var group in parts.GroupBy(p => p.FullName))
            {
                if (!group.Any(p => p.IsUi))
                {
                    continue;
                }

                var main = group.OrderByDescending(p => p.Lines).First();
                var info = new UiClassInfo
                {
                    Project = project,
                    FullName = group.Key,
                    FilePath = main.FilePath,
                    Line = main.StartLine,
                    Lines = group.Sum(p => p.Lines),
                    Handlers = group.Sum(p => p.Handlers)
                };
                result.Add(info);

                var severity = SeverityFor(info.Lines);
                if (severity.HasValue)
                {
                    findings.Add(new Finding(RuleCatalog.FormTooLarge, severity.Value,
                        "'" + info.FullName + "' ha " + info.Lines + " righe di code-behind e " + info.Handlers + " event handler.",
                        info.FilePath, info.Line, info.FullName) { Project = project });
                }
            }

            return result;
        }

        private static Severity? SeverityFor(int lines)
        {
            if (lines >= HugeLines)
            {
                return Severity.High;
            }

            if (lines >= VeryLargeLines)
            {
                return Severity.Medium;
            }

            return lines >= LargeLines ? (Severity?)Severity.Low : null;
        }
    }
}
