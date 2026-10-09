using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SolutionDoctor.Presentation.Mvvm;
using SolutionDoctor.Presentation.Services;
using Xunit;

namespace SolutionDoctor.Presentation.Tests
{
    public class FileSettingsStoreTests
    {
        [Fact]
        public void SalvaECarica_RoundTrip()
        {
            using (var dir = new SolutionDoctor.Core.Tests.TempDirectory())
            {
                var store = new FileSettingsStore(Path.Combine(dir.Path, "cartella", "settings.txt"));

                store.Save(new AppSettings { LastPath = @"C:\Progetti\App=1\App.sln", UseGit = false, GitMonths = 24 });
                var loaded = store.Load();

                Assert.Equal(@"C:\Progetti\App=1\App.sln", loaded.LastPath); // il '=' nel valore non rompe la lettura
                Assert.False(loaded.UseGit);
                Assert.Equal(24, loaded.GitMonths);
            }
        }

        [Fact]
        public void FileMancante_ValoriPredefiniti()
        {
            using (var dir = new SolutionDoctor.Core.Tests.TempDirectory())
            {
                var loaded = new FileSettingsStore(Path.Combine(dir.Path, "non-c-e.txt")).Load();

                Assert.Equal(string.Empty, loaded.LastPath);
                Assert.True(loaded.UseGit);
                Assert.Equal(12, loaded.GitMonths);
            }
        }

        [Fact]
        public void FileDanneggiato_SiIgnoranoLeRigheNonValide()
        {
            using (var dir = new SolutionDoctor.Core.Tests.TempDirectory())
            {
                var path = dir.Write("settings.txt", "spazzatura\n=senza chiave\nUseGit=forse\nGitMonths=-3\nGitMonths=abc\nLastPath=C:\\x.sln\nChiaveSconosciuta=1\n");

                var loaded = new FileSettingsStore(path).Load();

                Assert.Equal(@"C:\x.sln", loaded.LastPath);
                Assert.True(loaded.UseGit);
                Assert.Equal(12, loaded.GitMonths);
            }
        }

        [Fact]
        public void Salvataggio_ImpossibileNonSollevaEccezioni()
        {
            using (var dir = new SolutionDoctor.Core.Tests.TempDirectory())
            {
                // Il "file" delle impostazioni coincide con una cartella esistente: la scrittura fallisce.
                var path = Path.Combine(dir.Path, "settings.txt");
                Directory.CreateDirectory(path);

                new FileSettingsStore(path).Save(new AppSettings());
            }
        }
    }

    public class CommandTests
    {
        [Fact]
        public void RelayCommand_EseguiSoloSePermesso_ENotificaIlCambio()
        {
            var allowed = false;
            var executed = 0;
            var command = new RelayCommand(() => executed++, () => allowed);
            var raised = 0;
            command.CanExecuteChanged += (s, e) => raised++;

            command.Execute(null);
            Assert.Equal(0, executed);

            allowed = true;
            command.RaiseCanExecuteChanged();
            command.Execute(null);

            Assert.Equal(1, executed);
            Assert.Equal(1, raised);
            Assert.True(command.CanExecute(null));
        }

        [Fact]
        public async Task AsyncRelayCommand_NonRientrante_ERiabilitaAllaFine()
        {
            var gate = new TaskCompletionSource<bool>();
            var started = 0;
            var command = new AsyncRelayCommand(async () => { started++; await gate.Task; }, null, ex => { });

            command.Execute(null);
            command.Execute(null); // ignorato: il primo è ancora in corso
            Assert.Equal(1, started);
            Assert.False(command.CanExecute(null));

            gate.SetResult(true);
            for (var i = 0; i < 100 && !command.CanExecute(null); i++)
            {
                await Task.Delay(10);
            }

            Assert.True(command.CanExecute(null));
        }

        [Fact]
        public async Task AsyncRelayCommand_InoltraLeEccezioniAlGestoreDiErrore()
        {
            Exception received = null;
            var command = new AsyncRelayCommand(() => { throw new InvalidOperationException("boom"); }, null, ex => received = ex);

            command.Execute(null);
            for (var i = 0; i < 100 && received == null; i++)
            {
                await Task.Delay(10);
            }

            Assert.Equal("boom", received.Message);
            Assert.True(command.CanExecute(null));
        }
    }

    public class CoreAnalyzerTests
    {
        [Fact]
        public async Task EseguiLAnalisiVeraEHonoraIlTokenGiaAnnullato()
        {
            using (var dir = new SolutionDoctor.Core.Tests.TempDirectory())
            {
                SolutionDoctor.Core.Tests.LegacySolution.Create(dir);
                var options = new SolutionDoctor.Core.Analysis.AnalysisOptions { UseGit = false };

                var report = await new CoreAnalyzer().AnalyzeAsync(dir.Path, options, null, CancellationToken.None);
                Assert.Equal(2, report.Solution.Projects.Count);

                var cts = new CancellationTokenSource();
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => new CoreAnalyzer().AnalyzeAsync(dir.Path, options, null, cts.Token));
            }
        }
    }
}
