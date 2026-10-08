using System.Collections.Generic;
using System.Linq;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Model;
using Xunit;

namespace SolutionDoctor.Core.Tests
{
    public class BacklogBuilderTests
    {
        private static Finding F(string rule, Severity severity, string subject, string file)
        {
            return new Finding(rule, severity, "m", file, 1, subject) { Project = "App" };
        }

        [Fact]
        public void PunteggioESommaDeiPesi_ProgettiEsclusi()
        {
            var findings = new[]
            {
                F("WF004", Severity.High, "A.Form1", "Form1.cs"),
                F("WF002", Severity.Medium, "A.Form1", "Form1.cs"),
                F("WF005", Severity.Low, "A.Form1", "Form1.cs"),
                F("WF006", Severity.Info, "A.Form1", "Form1.cs"),
                F("PJ001", Severity.Medium, "App", "App.csproj")
            };

            var item = Assert.Single(BacklogBuilder.Build(findings, new UiClassInfo[0], null));

            Assert.Equal("A.Form1", item.Subject);
            Assert.Equal(10 + 4 + 2 + 1, item.Score);
            Assert.Equal(4, item.FindingCount);
            Assert.Equal("WF002×1, WF004×1, WF005×1, WF006×1", item.Rules);
            Assert.Equal(17, item.Priority);
        }

        [Fact]
        public void LaFrequenzaDiModificaAlzaLaPriorita_FileEDesignerInsieme()
        {
            var findings = new[]
            {
                F("WF004", Severity.High, "A.Calma", "Calma.cs"),
                F("WF004", Severity.High, "A.Attiva", "Attiva.cs")
            };
            var churn = new Dictionary<string, int> { { "Attiva.cs", 10 }, { "Attiva.Designer.cs", 5 }, { "Calma.cs", 0 } };

            var backlog = BacklogBuilder.Build(findings, new UiClassInfo[0], churn);

            Assert.Equal("A.Attiva", backlog[0].Subject);
            Assert.Equal(15, backlog[0].Churn);
            Assert.Equal(25, backlog[0].Priority); // 10 × (1 + 15/10)
            Assert.Equal(10, backlog[1].Priority);
        }

        [Fact]
        public void IlTettoAiCommitLimitaLAmplificazione()
        {
            var findings = new[] { F("WF004", Severity.High, "A.X", "X.cs") };

            var item = BacklogBuilder.Build(findings, new UiClassInfo[0], new Dictionary<string, int> { { "X.cs", 5000 } }).Single();

            Assert.Equal(60, item.Priority); // 10 × (1 + 50/10)
        }

        [Fact]
        public void LeMetricheDellaFormVengonoDalleClassiUi()
        {
            var findings = new[] { F("WF001", Severity.Low, "A.Form1", "Form1.cs") };
            var ui = new[] { new UiClassInfo { Project = "App", FullName = "A.Form1", FilePath = "Form1.cs", Lines = 450, Handlers = 12 } };

            var item = BacklogBuilder.Build(findings, ui, null).Single();

            Assert.Equal(450, item.Lines);
            Assert.Equal(12, item.Handlers);
        }
    }
}
