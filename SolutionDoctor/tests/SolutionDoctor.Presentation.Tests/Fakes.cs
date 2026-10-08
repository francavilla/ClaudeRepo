using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Model;
using SolutionDoctor.Presentation.Services;

namespace SolutionDoctor.Presentation.Tests
{
    internal sealed class FakeDialogs : IDialogService
    {
        public string PickedFile;
        public string PickedFolder;
        public string ReportPath;
        public string LastSuggestedName;
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Opened = new List<string>();
        public readonly List<string> Revealed = new List<string>();
        public readonly List<string> OpenedFolders = new List<string>();
        public string Clipboard;

        public string PickSolutionOrProject(string initialPath)
        {
            return PickedFile;
        }

        public string PickFolder(string initialPath)
        {
            return PickedFolder;
        }

        public string PickReportFile(string suggestedFileName)
        {
            LastSuggestedName = suggestedFileName;
            return ReportPath;
        }

        public void ShowError(string message)
        {
            Errors.Add(message);
        }

        public void OpenFile(string path)
        {
            Opened.Add(path);
        }

        public void RevealInFolder(string path)
        {
            Revealed.Add(path);
        }

        public void OpenFolder(string path)
        {
            OpenedFolders.Add(path);
        }

        public void CopyToClipboard(string text)
        {
            Clipboard = text;
        }
    }

    internal sealed class FakeSettings : ISettingsStore
    {
        public AppSettings Stored = new AppSettings();
        public int SaveCount;

        public AppSettings Load()
        {
            return Stored;
        }

        public void Save(AppSettings settings)
        {
            Stored = settings;
            SaveCount++;
        }
    }

    /// <summary>Analizzatore finto: esegue una funzione fornita dal test.</summary>
    internal sealed class FakeAnalyzer : IAnalyzer
    {
        private readonly Func<string, AnalysisOptions, CancellationToken, Task<AnalysisReport>> _run;

        public FakeAnalyzer(Func<string, AnalysisOptions, CancellationToken, Task<AnalysisReport>> run)
        {
            _run = run;
        }

        public string LastPath;
        public AnalysisOptions LastOptions;

        public Task<AnalysisReport> AnalyzeAsync(string path, AnalysisOptions options, IProgress<string> progress, CancellationToken cancellationToken)
        {
            LastPath = path;
            LastOptions = options;
            return _run(path, options, cancellationToken);
        }
    }
}
