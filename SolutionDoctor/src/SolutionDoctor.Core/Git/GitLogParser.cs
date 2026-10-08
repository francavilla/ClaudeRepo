using System;
using System.Collections.Generic;

namespace SolutionDoctor.Core.Git
{
    /// <summary>Interpreta l'output di <c>git log --name-only --pretty=format:</c>: un file per riga, uno per ogni commit che lo tocca.</summary>
    public static class GitLogParser
    {
        public static Dictionary<string, int> Parse(string output)
        {
            var churn = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(output))
            {
                return churn;
            }

            foreach (var raw in output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var path = raw.Trim().Replace('\\', '/');
                if (path.Length == 0)
                {
                    continue;
                }

                int count;
                churn.TryGetValue(path, out count);
                churn[path] = count + 1;
            }

            return churn;
        }
    }
}
