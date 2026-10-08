using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Git;
using SolutionDoctor.Core.Model;
using Xunit;

namespace SolutionDoctor.Core.Tests
{
    public class SolutionAnalyzerTests
    {
        private sealed class FakeChurn : IChurnProvider
        {
            private readonly IDictionary<string, int> _churn;

            public FakeChurn(IDictionary<string, int> churn)
            {
                _churn = churn;
            }

            public string RequestedDirectory;
            public int RequestedMonths;

            public IDictionary<string, int> TryGetChurn(string directory, int months)
            {
                RequestedDirectory = directory;
                RequestedMonths = months;
                return _churn;
            }
        }

        [Fact]
        public void SolutionLegacy_TrovaITuttiIProblemiAttesi()
        {
            using (var dir = new TempDirectory())
            {
                LegacySolution.Create(dir);
                var churn = new FakeChurn(new Dictionary<string, int> { { "App/Form1.cs", 20 } });

                var report = new SolutionAnalyzer().Analyze(dir.Path, new AnalysisOptions { ChurnProvider = churn, GitMonths = 6 });

                // Inventario
                Assert.Equal(new[] { "App", "Lib" }, report.Solution.Projects.Select(p => p.Name));
                Assert.Equal(new[] { "Lib", "App" }, report.MigrationOrder.Select(p => p.Name));
                Assert.Contains(report.Findings, f => f.RuleId == RuleCatalog.PackagesConfig && f.Project == "App");

                // Smell: solo Form1.cs e Servizio.cs; obj/ e Designer esclusi, Lib non è WinForms.
                var smells = report.Findings.Where(f => f.RuleId.StartsWith("WF")).ToList();
                Assert.DoesNotContain(smells, f => f.FilePath.Contains("obj/") || f.FilePath.Contains("Designer"));
                Assert.DoesNotContain(smells, f => f.Project == "Lib");
                Assert.Contains(smells, f => f.RuleId == RuleCatalog.ConcatenatedSql && f.FilePath == "App/Form1.cs" && f.Line == 6);
                Assert.Equal(2, smells.Count(f => f.RuleId == RuleCatalog.DoEvents));
                Assert.Contains(smells, f => f.RuleId == RuleCatalog.LongHandler && f.Message.Contains("btnAltro_Click"));
                Assert.Contains(smells, f => f.RuleId == RuleCatalog.FormTooLarge && f.Subject == "App.Form1");

                // Form
                var form = Assert.Single(report.UiClasses);
                Assert.Equal("App.Form1", form.FullName);
                Assert.Equal(2, form.Handlers);

                // Ordinamento per gravità e backlog con la cronologia git
                Assert.Equal(Severity.High, report.Findings.First().Severity);
                Assert.True(report.ChurnAvailable);
                Assert.Equal(dir.Path, churn.RequestedDirectory);
                Assert.Equal(6, churn.RequestedMonths);
                Assert.Equal("App.Form1", report.Backlog[0].Subject);
                Assert.Equal(20, report.Backlog[0].Churn);
                Assert.Equal("App.Servizio", report.Backlog.Last().Subject);
            }
        }

        [Fact]
        public void SenzaGit_PrioritaBasataSoloSuiProblemi()
        {
            using (var dir = new TempDirectory())
            {
                LegacySolution.Create(dir);
                var churn = new FakeChurn(new Dictionary<string, int> { { "App/Form1.cs", 20 } });

                var report = new SolutionAnalyzer().Analyze(dir.Path, new AnalysisOptions { UseGit = false, ChurnProvider = churn });

                Assert.False(report.ChurnAvailable);
                Assert.Null(churn.RequestedDirectory);
                Assert.All(report.Backlog, item => Assert.Equal(0, item.Churn));
            }
        }

        [Fact]
        public void GitNonDisponibile_AnalisiComunqueCompleta()
        {
            using (var dir = new TempDirectory())
            {
                LegacySolution.Create(dir);

                var report = new SolutionAnalyzer().Analyze(dir.Path, new AnalysisOptions { ChurnProvider = new FakeChurn(null) });

                Assert.False(report.ChurnAvailable);
                Assert.NotEmpty(report.Backlog);
            }
        }

        [Fact]
        public void SingoloCsproj_RelativiAllaSuaCartella()
        {
            using (var dir = new TempDirectory())
            {
                LegacySolution.Create(dir);

                var report = new SolutionAnalyzer().Analyze(
                    System.IO.Path.Combine(dir.Path, "App", "App.csproj"), new AnalysisOptions { UseGit = false });

                Assert.Single(report.Solution.Projects);
                Assert.Contains(report.Findings, f => f.FilePath == "Form1.cs");
            }
        }

        [Fact]
        public void VersioneDelloStrumento_NonVuota()
        {
            Assert.Matches(@"^\d+\.\d+\.\d+", SolutionAnalyzer.ToolVersion);
        }

        /// <summary>Avanzamento sincrono, per verificare l'ordine dei messaggi (Progress&lt;T&gt; li posterebbe sul thread pool).</summary>
        private sealed class SyncProgress : IProgress<string>
        {
            private readonly Action<string> _onReport;

            public SyncProgress(Action<string> onReport)
            {
                _onReport = onReport;
            }

            public void Report(string value)
            {
                _onReport(value);
            }
        }

        [Fact]
        public void Avanzamento_RiportaLeFasiNellOrdine()
        {
            using (var dir = new TempDirectory())
            {
                LegacySolution.Create(dir);
                var messages = new List<string>();

                new SolutionAnalyzer().Analyze(
                    dir.Path,
                    new AnalysisOptions { ChurnProvider = new FakeChurn(new Dictionary<string, int>()) },
                    new SyncProgress(messages.Add),
                    CancellationToken.None);

                Assert.Equal(
                    new[]
                    {
                        "Lettura di solution e progetti…",
                        "Analisi del progetto App (1 di 2)…",
                        "Analisi del progetto Lib (2 di 2)…",
                        "Lettura della cronologia git…",
                        "Calcolo delle priorità…"
                    },
                    messages);
            }
        }

        [Fact]
        public void TokenGiaAnnullato_SollevaOperationCanceledException()
        {
            using (var dir = new TempDirectory())
            {
                LegacySolution.Create(dir);
                var cts = new CancellationTokenSource();
                cts.Cancel();

                Assert.Throws<OperationCanceledException>(() =>
                    new SolutionAnalyzer().Analyze(dir.Path, new AnalysisOptions { UseGit = false }, null, cts.Token));
            }
        }

        [Fact]
        public void AnnullamentoDuranteLAnalisi_InterrompeSenzaRisultatoParziale()
        {
            using (var dir = new TempDirectory())
            {
                LegacySolution.Create(dir);
                var cts = new CancellationTokenSource();
                var progress = new SyncProgress(message =>
                {
                    if (message.StartsWith("Analisi del progetto App", StringComparison.Ordinal))
                    {
                        cts.Cancel(); // l'utente preme Annulla mentre si analizza il primo progetto
                    }
                });

                Assert.Throws<OperationCanceledException>(() =>
                    new SolutionAnalyzer().Analyze(dir.Path, new AnalysisOptions { UseGit = false }, progress, cts.Token));
            }
        }

        [Fact]
        public void SenzaAvanzamentoNeAnnullamento_ComportamentoInvariato()
        {
            using (var dir = new TempDirectory())
            {
                LegacySolution.Create(dir);
                var options = new AnalysisOptions { UseGit = false, Now = new DateTime(2026, 10, 8) };

                var plain = new SolutionAnalyzer().Analyze(dir.Path, options);
                var full = new SolutionAnalyzer().Analyze(dir.Path, options, null, CancellationToken.None);

                Assert.Equal(plain.Findings.Count, full.Findings.Count);
                Assert.Equal(
                    SolutionDoctor.Core.Reporting.MarkdownReportWriter.Write(plain),
                    SolutionDoctor.Core.Reporting.MarkdownReportWriter.Write(full));
            }
        }
    }
}
