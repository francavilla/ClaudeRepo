using System;
using System.Threading.Tasks;
using SolutionDoctor.Presentation.Services;
using SolutionDoctor.Presentation.ViewModels;

namespace SolutionDoctor.Presentation.Tests
{
    /// <summary>ViewModel pronto all'uso con i servizi finti, e un'analisi vera su una solution di prova.</summary>
    internal sealed class TestEnvironment : IDisposable
    {
        public TestEnvironment(IAnalyzer analyzer = null)
        {
            Directory = new SolutionDoctor.Core.Tests.TempDirectory();
            Dialogs = new FakeDialogs();
            Settings = new FakeSettings();
            ViewModel = new MainViewModel(analyzer ?? new CoreAnalyzer(), Dialogs, Settings, "1.1.0");
        }

        public SolutionDoctor.Core.Tests.TempDirectory Directory { get; private set; }

        public FakeDialogs Dialogs { get; private set; }

        public FakeSettings Settings { get; private set; }

        public MainViewModel ViewModel { get; private set; }

        /// <summary>Crea la solution legacy di prova e la analizza (senza git).</summary>
        public async Task AnalyzeLegacySolutionAsync()
        {
            SolutionDoctor.Core.Tests.LegacySolution.Create(Directory);
            ViewModel.UseGit = false;
            ViewModel.InputPath = System.IO.Path.Combine(Directory.Path, "Legacy.sln");
            await ViewModel.AnalyzeAsync();
        }

        public void Dispose()
        {
            Directory.Dispose();
        }
    }
}
