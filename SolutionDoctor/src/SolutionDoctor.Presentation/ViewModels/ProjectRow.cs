using System.Linq;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Presentation.ViewModels
{
    public sealed class ProjectRow
    {
        public ProjectRow(ProjectInfo project, int issueCount)
        {
            Name = project.Name;
            RelativePath = project.RelativePath;
            Frameworks = FrameworksText(project);
            Format = project.IsSdkStyle ? "SDK-style" : "classico";
            PackagesText = project.Packages.Count + (project.UsesPackagesConfig ? " (packages.config)" : string.Empty);
            OutputType = project.OutputType ?? string.Empty;
            WinFormsText = project.IsWinForms ? "sì" : "no";
            IssueCount = issueCount;
        }

        public string Name { get; private set; }

        public string RelativePath { get; private set; }

        public string Frameworks { get; private set; }

        public string Format { get; private set; }

        public string PackagesText { get; private set; }

        public string OutputType { get; private set; }

        public string WinFormsText { get; private set; }

        public int IssueCount { get; private set; }

        internal static string FrameworksText(ProjectInfo project)
        {
            return project.TargetFrameworks.Count > 0 ? string.Join(", ", project.TargetFrameworks) : "?";
        }
    }
}
