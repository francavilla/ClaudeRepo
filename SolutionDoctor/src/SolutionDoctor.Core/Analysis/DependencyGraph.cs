using System;
using System.Collections.Generic;
using System.Linq;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Core.Analysis
{
    /// <summary>
    /// Grafo dei riferimenti tra i progetti della solution (un arco da A a B se A referenzia B).
    /// L'ordine di migrazione consigliato parte dalle foglie: prima i progetti che non dipendono da nessuno.
    /// </summary>
    public sealed class DependencyGraph
    {
        private readonly Dictionary<ProjectInfo, List<ProjectInfo>> _dependencies;

        private DependencyGraph(Dictionary<ProjectInfo, List<ProjectInfo>> dependencies)
        {
            _dependencies = dependencies;
        }

        /// <summary>Progetti in ordine di migrazione (dipendenze prima di chi le usa).</summary>
        public List<ProjectInfo> MigrationOrder { get; private set; } = new List<ProjectInfo>();

        /// <summary>Progetti che non si riesce a ordinare perché in (o dipendenti da) un ciclo di riferimenti.</summary>
        public List<ProjectInfo> CycleMembers { get; private set; } = new List<ProjectInfo>();

        public IReadOnlyList<ProjectInfo> DependenciesOf(ProjectInfo project)
        {
            return _dependencies[project];
        }

        public static DependencyGraph Build(IEnumerable<ProjectInfo> projects)
        {
            var all = projects.ToList();
            var byPath = new Dictionary<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var project in all)
            {
                byPath[project.Path] = project;
            }

            var dependencies = new Dictionary<ProjectInfo, List<ProjectInfo>>();
            foreach (var project in all)
            {
                dependencies[project] = project.ProjectReferences
                    .Select(path => { ProjectInfo found; return byPath.TryGetValue(path, out found) ? found : null; })
                    .Where(p => p != null && p != project)
                    .Distinct()
                    .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var graph = new DependencyGraph(dependencies);
            graph.Sort(all);
            return graph;
        }

        // Algoritmo di Kahn: a parità di condizioni si procede in ordine alfabetico, per un risultato stabile.
        private void Sort(List<ProjectInfo> all)
        {
            var remaining = all.ToDictionary(p => p, p => _dependencies[p].Count);
            var ready = new SortedSet<ProjectInfo>(
                all.Where(p => remaining[p] == 0),
                Comparer<ProjectInfo>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase) != 0
                    ? string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
                    : string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase)));

            while (ready.Count > 0)
            {
                var next = ready.Min;
                ready.Remove(next);
                MigrationOrder.Add(next);

                foreach (var dependent in all.Where(p => _dependencies[p].Contains(next)))
                {
                    remaining[dependent]--;
                    if (remaining[dependent] == 0)
                    {
                        ready.Add(dependent);
                    }
                }
            }

            CycleMembers = all.Where(p => !MigrationOrder.Contains(p))
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
