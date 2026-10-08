using System.Collections.Generic;
using System.Linq;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Model;
using Xunit;

namespace SolutionDoctor.Core.Tests
{
    public class UiClassAggregatorTests
    {
        private static ClassPart Part(string name, bool ui, int start, int end, int handlers, string file)
        {
            return new ClassPart { FullName = name, IsUi = ui, StartLine = start, EndLine = end, Handlers = handlers, FilePath = file };
        }

        [Fact]
        public void PartiPartial_SiSommano()
        {
            var findings = new List<Finding>();

            var result = UiClassAggregator.Aggregate("App", new[]
            {
                Part("A.Main", true, 10, 209, 5, "Main.cs"),
                Part("A.Main", false, 3, 102, 2, "Main.Logic.cs")
            }, findings);

            var info = Assert.Single(result);
            Assert.Equal(300, info.Lines);
            Assert.Equal(7, info.Handlers);
            Assert.Equal("Main.cs", info.FilePath);
            Assert.Equal(10, info.Line);
            Assert.Empty(findings); // 300 righe < 400: sotto soglia
        }

        [Theory]
        [InlineData(399, null)]
        [InlineData(400, Severity.Low)]
        [InlineData(799, Severity.Low)]
        [InlineData(800, Severity.Medium)]
        [InlineData(1499, Severity.Medium)]
        [InlineData(1500, Severity.High)]
        public void SogliaDiDimensione(int lines, Severity? expected)
        {
            var findings = new List<Finding>();

            UiClassAggregator.Aggregate("App", new[] { Part("A.Big", true, 1, lines, 0, "Big.cs") }, findings);

            if (expected == null)
            {
                Assert.Empty(findings);
            }
            else
            {
                var finding = Assert.Single(findings);
                Assert.Equal(RuleCatalog.FormTooLarge, finding.RuleId);
                Assert.Equal(expected.Value, finding.Severity);
                Assert.Equal("App", finding.Project);
            }
        }

        [Fact]
        public void ClassiNonUi_Ignorate()
        {
            var findings = new List<Finding>();

            var result = UiClassAggregator.Aggregate("App", new[] { Part("A.Service", false, 1, 5000, 0, "S.cs") }, findings);

            Assert.Empty(result);
            Assert.Empty(findings);
        }
    }
}
