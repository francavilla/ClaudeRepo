using System.Collections.Generic;
using System.Linq;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Presentation.ViewModels
{
    /// <summary>Un passo dell'ordine di migrazione consigliato.</summary>
    public sealed class MigrationStepRow
    {
        public MigrationStepRow(int position, ProjectInfo project, IEnumerable<ProjectInfo> dependencies)
        {
            Position = position;
            Name = project.Name;
            Frameworks = ProjectRow.FrameworksText(project);
            var names = dependencies.Select(d => d.Name).ToList();
            DependsOn = names.Count == 0 ? "nessuna dipendenza" : "dipende da " + string.Join(", ", names);
        }

        public int Position { get; private set; }

        public string Name { get; private set; }

        public string Frameworks { get; private set; }

        public string DependsOn { get; private set; }
    }
}
