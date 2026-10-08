using System;
using System.Globalization;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Model;
using SolutionDoctor.Core.Reporting;
using Xunit;

namespace SolutionDoctor.Core.Tests
{
    public class MarkdownReportWriterTests
    {
        private static string Render(TempDirectory dir)
        {
            LegacySolution.Create(dir);
            var report = new SolutionAnalyzer().Analyze(dir.Path, new AnalysisOptions { UseGit = false, Now = new DateTime(2026, 10, 8, 14, 30, 0) });
            return MarkdownReportWriter.Write(report);
        }

        [Fact]
        public void ContieneTutteLeSezioni()
        {
            using (var dir = new TempDirectory())
            {
                var markdown = Render(dir);

                Assert.Contains("# SolutionDoctor — Legacy", markdown);
                Assert.Contains("Generato il 2026-10-08 14:30", markdown);
                Assert.Contains("## Riepilogo", markdown);
                Assert.Contains("## Da dove cominciare", markdown);
                Assert.Contains("| 1 | App.Form1 | App |", markdown);
                Assert.Contains("## Progetti", markdown);
                Assert.Contains("(packages.config)", markdown);
                Assert.Contains("## Ordine di migrazione consigliato", markdown);
                Assert.Contains("1. Lib", markdown);
                Assert.Contains("```mermaid", markdown);
                Assert.Contains("P0 --> P1", markdown);
                Assert.Contains("### WF004 — Comando SQL costruito per concatenazione (1)", markdown);
                Assert.Contains("`App/Form1.cs:6`", markdown);
                Assert.Contains("## Come intervenire", markdown);
                Assert.Contains("priorità basata solo sui problemi", markdown);
            }
        }

        [Fact]
        public void SolutionSenzaProblemi_ReportPulito()
        {
            using (var dir = new TempDirectory())
            {
                dir.Write("Lib/Lib.csproj", Samples.LibProject());

                var report = new SolutionAnalyzer().Analyze(dir.Path, new AnalysisOptions { UseGit = false });
                var markdown = MarkdownReportWriter.Write(report);

                Assert.Contains("Nessuna classe con problemi nel codice.", markdown);
                Assert.Contains("Nessun problema rilevato.", markdown);
                Assert.DoesNotContain("## Come intervenire", markdown);
                Assert.DoesNotContain("mermaid", markdown);
            }
        }

        [Fact]
        public void IlSimboloPipeNeiNomiNonRompeLeTabelle()
        {
            // Il report si costruisce a mano: su Windows '|' non è ammesso nei nomi di file.
            var project = new ProjectInfo { Name = "A|B", Path = "/s/A|B.csproj", RelativePath = "A|B.csproj", OutputType = "Library" };
            var report = new AnalysisReport { ToolVersion = "1.0.0", GeneratedAt = new DateTime(2026, 10, 8), Solution = new SolutionInfo { Name = "S" } };
            report.Solution.Projects.Add(project);
            report.MigrationOrder.Add(project);

            var markdown = MarkdownReportWriter.Write(report);

            Assert.Contains("| A\\|B |", markdown);
        }

        [Fact]
        public void IlReportNonDipendeDallaCulturaDelSistema()
        {
            using (var dir = new TempDirectory())
            {
                var previous = CultureInfo.CurrentCulture;
                try
                {
                    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                    var reference = Render(dir);

                    // Cultura con virgola decimale e separatore delle migliaia diverso.
                    CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                    var german = MarkdownReportWriter.Write(
                        new SolutionAnalyzer().Analyze(dir.Path, new AnalysisOptions { UseGit = false, Now = new DateTime(2026, 10, 8, 14, 30, 0) }));

                    Assert.Equal(reference, german);
                }
                finally
                {
                    CultureInfo.CurrentCulture = previous;
                }
            }
        }
    }
}
