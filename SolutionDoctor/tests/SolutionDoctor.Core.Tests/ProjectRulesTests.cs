using System.Linq;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Inventory;
using SolutionDoctor.Core.Model;
using Xunit;

namespace SolutionDoctor.Core.Tests
{
    public class ProjectRulesTests
    {
        [Fact]
        public void ProgettoLegacy_SegnalaTuttiIProblemi()
        {
            using (var dir = new TempDirectory())
            {
                dir.Write("App/packages.config", Samples.PackagesConfig);
                var project = ProjectReader.Read(dir.Write("App/App.csproj", Samples.ClassicWinFormsProject), dir.Path);

                var findings = ProjectRules.Evaluate(project).ToList();

                Assert.Contains(findings, f => f.RuleId == RuleCatalog.PackagesConfig && f.Severity == Severity.Medium);
                Assert.Contains(findings, f => f.RuleId == RuleCatalog.ClassicProjectFormat);
                Assert.Contains(findings, f => f.RuleId == RuleCatalog.OutdatedFramework && f.Message.Contains("net452"));
                Assert.Contains(findings, f => f.RuleId == RuleCatalog.MigrationBlockers && f.Message.Contains("System.Runtime.Remoting") && f.Severity == Severity.High);
                Assert.Contains(findings, f => f.RuleId == RuleCatalog.MigrationBlockers && f.Message.Contains("System.Web") && f.Severity == Severity.Medium);
                Assert.Contains(findings, f => f.RuleId == RuleCatalog.MigrationBlockers && f.Message.Contains("1 riferimento COM"));
                Assert.Contains(findings, f => f.RuleId == RuleCatalog.PackagesConfig && f.Message.Contains("2 pacchetti"));
                Assert.All(findings, f => Assert.Equal("App", f.Project));
            }
        }

        [Fact]
        public void ProgettoModerno_NessunProblema()
        {
            using (var dir = new TempDirectory())
            {
                var project = ProjectReader.Read(dir.Write("App/App.csproj", Samples.SdkWinFormsProject), dir.Path);

                Assert.Empty(ProjectRules.Evaluate(project));
            }
        }
    }
}
