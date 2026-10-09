using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Model;
using SolutionDoctor.Presentation.Services;
using SolutionDoctor.Presentation.ViewModels;
using Xunit;

namespace SolutionDoctor.Presentation.Tests
{
    public class MainViewModelTests
    {
        // ---------------------------------------------------------------- Input

        [Fact]
        public void PercorsoVuoto_NonSegnalaErroreMaNonSiPuoAnalizzare()
        {
            using (var env = new TestEnvironment())
            {
                Assert.False(env.ViewModel.CanAnalyze);
                Assert.False(env.ViewModel.HasInputError);
                Assert.False(env.ViewModel.AnalyzeCommand.CanExecute(null));
            }
        }

        [Fact]
        public void PercorsoInesistente_SegnalaErrore()
        {
            using (var env = new TestEnvironment())
            {
                env.ViewModel.InputPath = Path.Combine(env.Directory.Path, "non-esiste.sln");

                Assert.Equal("Il percorso non esiste.", env.ViewModel.InputError);
                Assert.True(env.ViewModel.HasInputError);
                Assert.False(env.ViewModel.CanAnalyze);
            }
        }

        [Fact]
        public void FileConEstensioneNonValida_SegnalaErrore()
        {
            using (var env = new TestEnvironment())
            {
                env.ViewModel.InputPath = env.Directory.Write("note.txt", "x");

                Assert.Contains(".sln o .csproj", env.ViewModel.InputError);
            }
        }

        [Theory]
        [InlineData("sln")]
        [InlineData("csproj")]
        [InlineData("cartella")]
        [InlineData("con virgolette")]
        public void PercorsiValidi_AbilitanoLAnalisi(string kind)
        {
            using (var env = new TestEnvironment())
            {
                var file = env.Directory.Write("App/App.csproj", Samples());
                env.Directory.Write("My.sln", SolutionDoctor.Core.Tests.Samples.Solution("App\\App.csproj"));
                string input;
                switch (kind)
                {
                    case "sln": input = Path.Combine(env.Directory.Path, "My.sln"); break;
                    case "csproj": input = file; break;
                    case "cartella": input = env.Directory.Path; break;
                    default: input = "  \"" + file + "\"  "; break;
                }

                env.ViewModel.InputPath = input;

                Assert.Null(env.ViewModel.InputError);
                Assert.True(env.ViewModel.CanAnalyze);
                Assert.True(env.ViewModel.AnalyzeCommand.CanExecute(null));
            }
        }

        private static string Samples()
        {
            return SolutionDoctor.Core.Tests.Samples.LibProject();
        }

        [Fact]
        public void CambioDelPercorso_RiaggiornaLAbilitazioneDeiComandi()
        {
            using (var env = new TestEnvironment())
            {
                var raised = 0;
                env.ViewModel.AnalyzeCommand.CanExecuteChanged += (s, e) => raised++;

                env.ViewModel.InputPath = env.Directory.Path;

                Assert.True(raised > 0);
            }
        }

        [Fact]
        public void Sfoglia_ImpostaIlPercorsoScelto_EAnnullareLoLasciaInvariato()
        {
            using (var env = new TestEnvironment())
            {
                env.Dialogs.PickedFile = env.Directory.Write("App/App.csproj", Samples());
                env.ViewModel.BrowseFileCommand.Execute(null);
                Assert.Equal(env.Dialogs.PickedFile, env.ViewModel.InputPath);

                env.Dialogs.PickedFolder = null; // annullato
                env.ViewModel.BrowseFolderCommand.Execute(null);
                Assert.Equal(env.Dialogs.PickedFile, env.ViewModel.InputPath);

                env.Dialogs.PickedFolder = env.Directory.Path;
                env.ViewModel.BrowseFolderCommand.Execute(null);
                Assert.Equal(env.Directory.Path, env.ViewModel.InputPath);
            }
        }

        // ---------------------------------------------------------------- Analisi

        [Fact]
        public async Task Analisi_PopolaRiepilogoBacklogProblemiProgettiEMigrazione()
        {
            using (var env = new TestEnvironment())
            {
                await env.AnalyzeLegacySolutionAsync();
                var vm = env.ViewModel;

                Assert.True(vm.HasResult);
                Assert.False(vm.ShowEmptyState);
                Assert.False(vm.IsBusy);
                Assert.Null(vm.AnalysisError);
                Assert.Equal("SolutionDoctor — Legacy", vm.WindowTitle);
                Assert.Equal("Legacy", vm.SolutionName);

                Assert.Equal(2, vm.ProjectCount);
                Assert.Equal(1, vm.WinFormsProjectCount);
                Assert.Equal(1, vm.FormCount);
                Assert.True(vm.TotalFindingCount > 0);
                Assert.Equal(vm.TotalFindingCount, vm.HighCount + vm.MediumCount + vm.LowCount + vm.InfoCount);
                Assert.True(vm.HighCount >= 1);

                Assert.Equal("App.Form1", vm.Backlog[0].Subject);
                Assert.Equal(1, vm.Backlog[0].Rank);
                Assert.Same(vm.Backlog[0], vm.SelectedBacklogItem);
                Assert.NotEmpty(vm.SelectedBacklogFindings);
                Assert.All(vm.SelectedBacklogFindings, f => Assert.Equal("App.Form1", f.Subject));

                Assert.Equal(new[] { "App", "Lib" }, vm.Projects.Select(p => p.Name));
                Assert.Equal(new[] { "Lib", "App" }, vm.MigrationSteps.Select(s => s.Name));
                Assert.Equal("nessuna dipendenza", vm.MigrationSteps[0].DependsOn);
                Assert.Equal("dipende da Lib", vm.MigrationSteps[1].DependsOn);
                Assert.Null(vm.CycleWarning);
                Assert.False(vm.HasCycleWarning);

                Assert.Equal("Problemi (" + vm.TotalFindingCount + ")", vm.FindingsTabTitle);
                Assert.Equal("Progetti (2)", vm.ProjectsTabTitle);
                Assert.Contains("Cronologia git non usata", vm.ChurnNote);
                Assert.StartsWith("Analisi completata: 2 progetti, 1 form", vm.StatusText);
                Assert.Equal(ResultTab.Backlog, vm.SelectedTab);
            }
        }

        [Fact]
        public async Task Analisi_PassaAlLAnalizzatorePercorsoNormalizzatoEOpzioni()
        {
            using (var env = new TestEnvironment())
            {
                var analyzer = new FakeAnalyzer((p, o, t) => Task.FromResult(EmptyReport()));
                var vm = new MainViewModel(analyzer, env.Dialogs, env.Settings, "1.1.0");
                var file = env.Directory.Write("App/App.csproj", Samples());
                vm.InputPath = "  \"" + file + "\" ";
                vm.UseGit = false;
                vm.GitMonths = 24;

                await vm.AnalyzeAsync();

                Assert.Equal(file, analyzer.LastPath);
                Assert.False(analyzer.LastOptions.UseGit);
                Assert.Equal(24, analyzer.LastOptions.GitMonths);
            }
        }

        [Fact]
        public async Task Analisi_SalvaLeImpostazioni()
        {
            using (var env = new TestEnvironment())
            {
                await env.AnalyzeLegacySolutionAsync();

                Assert.Equal(1, env.Settings.SaveCount);
                Assert.Equal(Path.Combine(env.Directory.Path, "Legacy.sln"), env.Settings.Stored.LastPath);
                Assert.False(env.Settings.Stored.UseGit);
            }
        }

        [Fact]
        public async Task ErroreNoto_DiventaUnMessaggioInPaginaESenzaRisultato()
        {
            using (var env = new TestEnvironment())
            {
                var analyzer = new FakeAnalyzer((p, o, t) => { throw new InvalidOperationException("La cartella contiene più file .sln"); });
                var vm = new MainViewModel(analyzer, env.Dialogs, env.Settings, "1.1.0");
                vm.InputPath = env.Directory.Path;

                await vm.AnalyzeAsync();

                Assert.Equal("La cartella contiene più file .sln", vm.AnalysisError);
                Assert.True(vm.HasAnalysisError);
                Assert.Equal("Analisi non riuscita.", vm.StatusText);
                Assert.False(vm.HasResult);
                Assert.False(vm.IsBusy);
                Assert.Empty(env.Dialogs.Errors);
                Assert.Equal(0, env.Settings.SaveCount);
            }
        }

        [Fact]
        public async Task ErroreImprevisto_PassaDalComandoEMostraUnDialogo()
        {
            using (var env = new TestEnvironment())
            {
                var analyzer = new FakeAnalyzer((p, o, t) => { throw new NullReferenceException("bug"); });
                var vm = new MainViewModel(analyzer, env.Dialogs, env.Settings, "1.1.0");
                vm.InputPath = env.Directory.Path;

                vm.AnalyzeCommand.Execute(null);
                await WaitFor(() => env.Dialogs.Errors.Count > 0);

                Assert.Equal("bug", env.Dialogs.Errors[0]);
                await WaitFor(() => !vm.IsBusy);
                Assert.True(vm.AnalyzeCommand.CanExecute(null)); // il comando si è liberato
            }
        }

        [Fact]
        public async Task Annulla_InterrompeLAnalisi_EConservaIlRisultatoPrecedente()
        {
            using (var env = new TestEnvironment())
            {
                var started = new TaskCompletionSource<bool>();
                var first = true;
                var analyzer = new FakeAnalyzer(async (p, o, t) =>
                {
                    if (first)
                    {
                        first = false;
                        return EmptyReport();
                    }

                    started.SetResult(true);
                    await Task.Delay(Timeout.Infinite, t);
                    return null;
                });
                var vm = new MainViewModel(analyzer, env.Dialogs, env.Settings, "1.1.0");
                vm.InputPath = env.Directory.Path;
                await vm.AnalyzeAsync();
                Assert.True(vm.HasResult);

                var running = vm.AnalyzeAsync();
                await started.Task;
                Assert.True(vm.IsBusy);
                Assert.True(vm.CancelCommand.CanExecute(null));
                Assert.False(vm.AnalyzeCommand.CanExecute(null)); // non rientrante

                vm.CancelCommand.Execute(null);
                await running;

                Assert.Equal("Analisi annullata.", vm.StatusText);
                Assert.False(vm.IsBusy);
                Assert.True(vm.HasResult); // il risultato precedente resta visibile
                Assert.False(vm.CancelCommand.CanExecute(null));
                Assert.Equal(1, env.Settings.SaveCount);
            }
        }

        [Fact]
        public async Task AnalizzareDiNuovo_SostituisceIlRisultatoERipristinaIFiltri()
        {
            using (var env = new TestEnvironment())
            {
                await env.AnalyzeLegacySolutionAsync();
                env.ViewModel.SearchText = "DoEvents";
                env.ViewModel.SelectedTab = ResultTab.Findings;

                await env.ViewModel.AnalyzeAsync();

                Assert.Equal(string.Empty, env.ViewModel.SearchText);
                Assert.Equal(env.ViewModel.TotalFindingCount, env.ViewModel.Findings.Count);
                Assert.Equal(ResultTab.Backlog, env.ViewModel.SelectedTab);
            }
        }

        [Fact]
        public async Task RiferimentiCircolari_SonoSegnalati()
        {
            using (var env = new TestEnvironment())
            {
                env.Directory.Write("A/A.csproj", Ref("B"));
                env.Directory.Write("B/B.csproj", Ref("A"));
                env.Directory.Write("C/C.csproj", SolutionDoctor.Core.Tests.Samples.LibProject());
                env.ViewModel.UseGit = false;
                env.ViewModel.InputPath = env.Directory.Path;

                await env.ViewModel.AnalyzeAsync();

                Assert.True(env.ViewModel.HasCycleWarning);
                Assert.Contains("A, B", env.ViewModel.CycleWarning);
                Assert.Equal(new[] { "C" }, env.ViewModel.MigrationSteps.Select(s => s.Name));
            }
        }

        private static string Ref(string other)
        {
            return "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net48</TargetFramework></PropertyGroup>"
                + "<ItemGroup><ProjectReference Include=\"..\\" + other + "\\" + other + ".csproj\" /></ItemGroup></Project>";
        }

        [Fact]
        public async Task ProgettoIllegibile_DiventaUnAvviso()
        {
            using (var env = new TestEnvironment())
            {
                env.Directory.Write("Rotto/Rotto.csproj", "<Project><non chiuso>");
                env.Directory.Write("Lib/Lib.csproj", SolutionDoctor.Core.Tests.Samples.LibProject());
                env.ViewModel.UseGit = false;
                env.ViewModel.InputPath = env.Directory.Path;

                await env.ViewModel.AnalyzeAsync();

                Assert.True(env.ViewModel.HasWarnings);
                Assert.Contains("Rotto.csproj", env.ViewModel.WarningsText);
            }
        }

        // ---------------------------------------------------------------- Filtri

        [Fact]
        public async Task FiltroPerGravita_MostraSoloIProblemiAlLivelloRichiesto()
        {
            using (var env = new TestEnvironment())
            {
                await env.AnalyzeLegacySolutionAsync();
                var vm = env.ViewModel;
                var total = vm.TotalFindingCount;

                vm.SelectedSeverityFilter = vm.SeverityFilters.Single(f => f.Minimum == Severity.High);

                Assert.NotEmpty(vm.Findings);
                Assert.All(vm.Findings, f => Assert.Equal(Severity.High, f.Severity));
                Assert.Equal(vm.HighCount, vm.Findings.Count);
                Assert.Equal(vm.HighCount + " di " + total + " problemi", vm.FilteredCountText);
                Assert.True(vm.HasActiveFilters);
                Assert.Equal(total, vm.TotalFindingCount);
            }
        }

        [Fact]
        public async Task FiltroPerRegola_ERicerca_SiCombinano()
        {
            using (var env = new TestEnvironment())
            {
                await env.AnalyzeLegacySolutionAsync();
                var vm = env.ViewModel;

                Assert.Null(vm.RuleFilters[0].RuleId);
                Assert.Contains(vm.RuleFilters, r => r.RuleId == RuleCatalog.DoEvents && r.Label.StartsWith("WF003 — "));

                vm.SelectedRuleFilter = vm.RuleFilters.Single(r => r.RuleId == RuleCatalog.DoEvents);
                Assert.Equal(2, vm.Findings.Count);
                Assert.All(vm.Findings, f => Assert.Equal(RuleCatalog.DoEvents, f.RuleId));

                vm.SearchText = "servizio";
                Assert.Single(vm.Findings);
                Assert.Equal("App/Servizio.cs", vm.Findings[0].FilePath);

                vm.SearchText = "non-c'è-niente-del-genere";
                Assert.Empty(vm.Findings);
                Assert.True(vm.NoFindingsMatch);
            }
        }

        [Fact]
        public async Task AzzeraFiltri_RipristinaTuttiIProblemi()
        {
            using (var env = new TestEnvironment())
            {
                await env.AnalyzeLegacySolutionAsync();
                var vm = env.ViewModel;
                vm.SelectedSeverityFilter = vm.SeverityFilters.Last();
                vm.SelectedRuleFilter = vm.RuleFilters[1];
                vm.SearchText = "x";
                Assert.True(vm.ClearFiltersCommand.CanExecute(null));

                vm.ClearFiltersCommand.Execute(null);

                Assert.False(vm.HasActiveFilters);
                Assert.Equal(vm.TotalFindingCount, vm.Findings.Count);
                Assert.Same(vm.SeverityFilters[0], vm.SelectedSeverityFilter);
                Assert.Null(vm.SelectedRuleFilter.RuleId);
                Assert.Equal(string.Empty, vm.SearchText);
                Assert.False(vm.ClearFiltersCommand.CanExecute(null));
            }
        }

        [Fact]
        public async Task SeFiltraViaIlProblemaSelezionato_LaSelezioneSiAzzera()
        {
            using (var env = new TestEnvironment())
            {
                await env.AnalyzeLegacySolutionAsync();
                var vm = env.ViewModel;
                vm.SelectedFinding = vm.Findings.First(f => f.Severity == Severity.Medium);
                Assert.True(vm.HasSelectedFinding);

                vm.SelectedSeverityFilter = vm.SeverityFilters.Last();

                Assert.Null(vm.SelectedFinding);
                Assert.False(vm.HasSelectedFinding);
            }
        }

        // ---------------------------------------------------------------- Azioni

        [Fact]
        public async Task SalvaReport_ScriveIlMarkdownSulFileScelto()
        {
            using (var env = new TestEnvironment())
            {
                await env.AnalyzeLegacySolutionAsync();
                env.Dialogs.ReportPath = Path.Combine(env.Directory.Path, "report.md");

                Assert.True(env.ViewModel.SaveReportCommand.CanExecute(null));
                env.ViewModel.SaveReportCommand.Execute(null);

                Assert.Contains("# SolutionDoctor — Legacy", File.ReadAllText(env.Dialogs.ReportPath));
                Assert.Matches(@"^SolutionDoctor-Legacy-\d{8}\.md$", env.Dialogs.LastSuggestedName);
                Assert.Equal("Report salvato in " + env.Dialogs.ReportPath, env.ViewModel.StatusText);
            }
        }

        [Fact]
        public async Task SalvaReport_AnnullatoOImpossibile()
        {
            using (var env = new TestEnvironment())
            {
                await env.AnalyzeLegacySolutionAsync();
                var previous = env.ViewModel.StatusText;

                env.Dialogs.ReportPath = null; // l'utente annulla
                env.ViewModel.SaveReportCommand.Execute(null);
                Assert.Equal(previous, env.ViewModel.StatusText);
                Assert.Empty(env.Dialogs.Errors);

                env.Dialogs.ReportPath = Path.Combine(env.Directory.Path, "non", "esiste", "report.md");
                env.ViewModel.SaveReportCommand.Execute(null);
                Assert.Single(env.Dialogs.Errors);
                Assert.StartsWith("Impossibile salvare il report", env.Dialogs.Errors[0]);
            }
        }

        [Fact]
        public async Task CopiaReport_EApriCartella()
        {
            using (var env = new TestEnvironment())
            {
                await env.AnalyzeLegacySolutionAsync();

                env.ViewModel.CopyReportCommand.Execute(null);
                env.ViewModel.OpenSolutionFolderCommand.Execute(null);

                Assert.Contains("# SolutionDoctor — Legacy", env.Dialogs.Clipboard);
                Assert.Equal(new[] { env.Directory.Path }, env.Dialogs.OpenedFolders);
                Assert.Equal("Report copiato negli appunti.", env.ViewModel.StatusText);
            }
        }

        [Fact]
        public void SenzaRisultato_IComandiDelReportSonoDisabilitati()
        {
            using (var env = new TestEnvironment())
            {
                Assert.False(env.ViewModel.SaveReportCommand.CanExecute(null));
                Assert.False(env.ViewModel.CopyReportCommand.CanExecute(null));
                Assert.False(env.ViewModel.OpenSolutionFolderCommand.CanExecute(null));
                Assert.False(env.ViewModel.OpenFindingCommand.CanExecute(null));
                Assert.False(env.ViewModel.OpenBacklogFileCommand.CanExecute(null));
                Assert.True(env.ViewModel.ShowEmptyState);
                Assert.Equal("SolutionDoctor", env.ViewModel.WindowTitle);
            }
        }

        [Fact]
        public async Task ComandiSulProblemaSelezionato_UsanoIlPercorsoCompleto()
        {
            using (var env = new TestEnvironment())
            {
                await env.AnalyzeLegacySolutionAsync();
                var vm = env.ViewModel;
                vm.SelectedFinding = vm.Findings.First(f => f.RuleId == RuleCatalog.ConcatenatedSql);
                var expected = Path.Combine(env.Directory.Path, "App", "Form1.cs");

                vm.OpenFindingCommand.Execute(null);
                vm.RevealFindingCommand.Execute(null);
                vm.CopyFindingPathCommand.Execute(null);

                Assert.Equal(new[] { expected }, env.Dialogs.Opened);
                Assert.Equal(new[] { expected }, env.Dialogs.Revealed);
                Assert.Equal(expected, env.Dialogs.Clipboard);
                Assert.Equal("App/Form1.cs:6", vm.SelectedFinding.Location);
                Assert.Equal("WF004", vm.SelectedFinding.RuleId);
                Assert.False(string.IsNullOrEmpty(vm.SelectedFinding.Advice));
                Assert.Equal("Alta", vm.SelectedFinding.SeverityLabel);
            }
        }

        [Fact]
        public async Task ApriFileDelBacklog_UsaIlFileDellaClasse()
        {
            using (var env = new TestEnvironment())
            {
                await env.AnalyzeLegacySolutionAsync();

                env.ViewModel.OpenBacklogFileCommand.Execute(null);

                Assert.Equal(new[] { Path.Combine(env.Directory.Path, "App", "Form1.cs") }, env.Dialogs.Opened);
            }
        }

        // ---------------------------------------------------------------- Impostazioni

        [Fact]
        public void CaricaImpostazioni_RipristinaPercorsoEsistenteEOpzioni()
        {
            using (var env = new TestEnvironment())
            {
                var file = env.Directory.Write("App/App.csproj", Samples());
                env.Settings.Stored = new AppSettings { LastPath = file, UseGit = false, GitMonths = 6 };

                env.ViewModel.LoadSettings();

                Assert.Equal(file, env.ViewModel.InputPath);
                Assert.False(env.ViewModel.UseGit);
                Assert.Equal(6, env.ViewModel.GitMonths);
            }
        }

        [Fact]
        public void CaricaImpostazioni_IgnoraPercorsoNonPiuEsistenteEMesiNonValidi()
        {
            using (var env = new TestEnvironment())
            {
                env.Settings.Stored = new AppSettings { LastPath = Path.Combine(env.Directory.Path, "sparito.sln"), GitMonths = 7 };

                env.ViewModel.LoadSettings();

                Assert.Equal(string.Empty, env.ViewModel.InputPath);
                Assert.Equal(12, env.ViewModel.GitMonths);
            }
        }

        // ---------------------------------------------------------------- Sezioni e stato per il XAML

        [Fact]
        public void BarraDelleSezioni_UnaSolaAttivaEScrivereFalseNonCambiaNulla()
        {
            using (var env = new TestEnvironment())
            {
                var vm = env.ViewModel;
                var changed = new System.Collections.Generic.List<string>();
                vm.PropertyChanged += (s, e) => changed.Add(e.PropertyName);
                Assert.True(vm.IsBacklogTab);

                vm.IsFindingsTab = true;

                Assert.Equal(ResultTab.Findings, vm.SelectedTab);
                Assert.True(vm.IsFindingsTab);
                Assert.False(vm.IsBacklogTab);
                Assert.Contains("IsBacklogTab", changed);
                Assert.Contains("IsFindingsTab", changed);

                // WPF scrive false sul RadioButton che perde la selezione: va ignorato.
                vm.IsBacklogTab = false;
                Assert.Equal(ResultTab.Findings, vm.SelectedTab);

                vm.IsMigrationTab = true;
                vm.IsProjectsTab = true;
                Assert.Equal(ResultTab.Projects, vm.SelectedTab);
            }
        }

        [Fact]
        public async Task StatoDiAttesaEPresentazione_SeguonoLAnalisi()
        {
            using (var env = new TestEnvironment())
            {
                var gate = new TaskCompletionSource<AnalysisReport>();
                var analyzer = new FakeAnalyzer((p, o, t) => gate.Task);
                var vm = new MainViewModel(analyzer, env.Dialogs, env.Settings, "1.1.0");
                vm.InputPath = env.Directory.Path;
                Assert.True(vm.ShowEmptyState);
                Assert.False(vm.ShowBusyPlaceholder);

                var running = vm.AnalyzeAsync();

                Assert.True(vm.IsBusy);
                Assert.False(vm.IsIdle);
                Assert.False(vm.ShowEmptyState);
                Assert.True(vm.ShowBusyPlaceholder);

                gate.SetResult(EmptyReport());
                await running;

                Assert.False(vm.ShowBusyPlaceholder);
                Assert.False(vm.ShowEmptyState);
                Assert.True(vm.IsBacklogEmpty);
                Assert.False(vm.HasSelectedBacklogItem);
            }
        }

        [Fact]
        public async Task SelezioneNelBacklog_AggiornaIlDettaglio()
        {
            using (var env = new TestEnvironment())
            {
                await env.AnalyzeLegacySolutionAsync();
                var vm = env.ViewModel;
                Assert.True(vm.HasSelectedBacklogItem);
                Assert.False(vm.IsBacklogEmpty);

                vm.SelectedBacklogItem = null;

                Assert.False(vm.HasSelectedBacklogItem);
                Assert.Empty(vm.SelectedBacklogFindings);
                Assert.False(vm.OpenBacklogFileCommand.CanExecute(null));
            }
        }

        // ---------------------------------------------------------------- Supporto

        private static AnalysisReport EmptyReport()
        {
            var report = new AnalysisReport { ToolVersion = "1.1.0", GeneratedAt = new DateTime(2026, 10, 8), Solution = new SolutionInfo { Name = "Vuota", RootDirectory = Path.GetTempPath() } };
            return report;
        }

        private static async Task WaitFor(Func<bool> condition)
        {
            for (var i = 0; i < 200 && !condition(); i++)
            {
                await Task.Delay(10);
            }

            Assert.True(condition(), "condizione non raggiunta entro il tempo previsto");
        }
    }
}
