using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using BuildExe.Core.Analysis;
using BuildExe.Core.Execution;
using BuildExe.Core.Model;
using BuildExe.Core.Planning;
using BuildExe.Core.Toolchain;
using BuildExe.Mvvm;
using BuildExe.Services;

namespace BuildExe.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        private const int MaxLogLines = 20000;

        private readonly ProjectAnalyzer _analyzer;
        private readonly SolutionParser _solutionParser;
        private readonly BuildPlanner _planner;
        private readonly BuildService _buildService;
        private readonly IToolchainLocator _toolchainLocator;
        private readonly IDialogService _dialogs;

        private string _inputPath;
        private string _inputError;
        private bool _isSolution;
        private ProjectInfo _selectedProject;
        private TargetFramework _selectedTargetFramework;
        private string _outputDirectory;
        private bool _selfContained;
        private bool _singleFile;
        private string _runtimeIdentifier = "win-x64";
        private Toolchains _toolchains;
        private string _toolchainSummary = "Rilevamento degli strumenti di build in corso...";
        private BuildPlan _plan;
        private bool _isBusy;
        private string _statusText = "Pronto.";
        private string _lastExecutable;
        private CancellationTokenSource _cancellation;

        public MainViewModel(
            ProjectAnalyzer analyzer,
            SolutionParser solutionParser,
            BuildPlanner planner,
            BuildService buildService,
            IToolchainLocator toolchainLocator,
            IDialogService dialogs)
        {
            _analyzer = analyzer;
            _solutionParser = solutionParser;
            _planner = planner;
            _buildService = buildService;
            _toolchainLocator = toolchainLocator;
            _dialogs = dialogs;

            SolutionProjects = new ObservableCollection<ProjectInfo>();
            TargetFrameworks = new ObservableCollection<TargetFramework>();
            PlanMessages = new ObservableCollection<PlanMessage>();
            Log = new ObservableCollection<LogLine>();

            BrowseInputCommand = new RelayCommand(BrowseInput, () => !IsBusy);
            BrowseOutputCommand = new RelayCommand(BrowseOutput, () => !IsBusy);
            BuildCommand = new AsyncRelayCommand(BuildAsync, () => !IsBusy && Plan != null && Plan.CanBuild, OnUnexpectedError);
            CancelCommand = new RelayCommand(Cancel, () => IsBusy);
            OpenOutputCommand = new RelayCommand(OpenOutput, () => ResolvedOutputDirectory != null && Directory.Exists(ResolvedOutputDirectory));
            CopyLogCommand = new RelayCommand(CopyLog, () => Log.Count > 0);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy, OnUnexpectedError);
        }

        // ---------------------------------------------------------------- Comandi

        public ICommand BrowseInputCommand { get; private set; }

        public ICommand BrowseOutputCommand { get; private set; }

        public ICommand BuildCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        public ICommand OpenOutputCommand { get; private set; }

        public ICommand CopyLogCommand { get; private set; }

        public ICommand RefreshCommand { get; private set; }

        // ---------------------------------------------------------------- Input

        /// <summary>Percorso del progetto (.csproj/.vbproj/.fsproj) o della solution (.sln/.slnx).</summary>
        public string InputPath
        {
            get { return _inputPath; }
            set
            {
                if (SetProperty(ref _inputPath, value))
                {
                    LoadInput();
                }
            }
        }

        public string InputError
        {
            get { return _inputError; }
            private set { SetProperty(ref _inputError, value); }
        }

        public bool IsSolution
        {
            get { return _isSolution; }
            private set { SetProperty(ref _isSolution, value); }
        }

        /// <summary>Progetti eseguibili della solution.</summary>
        public ObservableCollection<ProjectInfo> SolutionProjects { get; private set; }

        public ProjectInfo SelectedProject
        {
            get { return _selectedProject; }
            set
            {
                if (SetProperty(ref _selectedProject, value))
                {
                    OnProjectChanged();
                }
            }
        }

        public bool HasProject
        {
            get { return SelectedProject != null; }
        }

        public ObservableCollection<TargetFramework> TargetFrameworks { get; private set; }

        public TargetFramework SelectedTargetFramework
        {
            get { return _selectedTargetFramework; }
            set
            {
                if (SetProperty(ref _selectedTargetFramework, value))
                {
                    OnPropertyChanged("IsNetCoreTarget");
                    UpdatePlan();
                }
            }
        }

        public bool IsMultiTarget
        {
            get { return TargetFrameworks.Count > 1; }
        }

        /// <summary>Le opzioni di deployment (self-contained, single file) valgono solo per .NET Core / .NET 5+.</summary>
        public bool IsNetCoreTarget
        {
            get { return SelectedTargetFramework != null && SelectedTargetFramework.Family == FrameworkFamily.NetCore && SelectedProject != null && SelectedProject.IsSdkStyle; }
        }

        public string TargetFrameworksText
        {
            get { return SelectedProject == null ? string.Empty : string.Join(", ", SelectedProject.TargetFrameworks.Select(t => t.DisplayName + " [" + t.Moniker + "]")); }
        }

        // ---------------------------------------------------------------- Output e opzioni

        /// <summary>Cartella di output; se relativa, è relativa alla cartella del progetto.</summary>
        public string OutputDirectory
        {
            get { return _outputDirectory; }
            set
            {
                if (SetProperty(ref _outputDirectory, value))
                {
                    OnPropertyChanged("ResolvedOutputDirectory");
                    UpdatePlan();
                }
            }
        }

        public string ResolvedOutputDirectory
        {
            get
            {
                if (string.IsNullOrWhiteSpace(OutputDirectory))
                {
                    return null;
                }

                try
                {
                    var path = Environment.ExpandEnvironmentVariables(OutputDirectory.Trim());
                    if (!Path.IsPathRooted(path) && SelectedProject != null)
                    {
                        path = Path.Combine(SelectedProject.Directory, path);
                    }

                    return Path.GetFullPath(path);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
                {
                    return null;
                }
            }
        }

        public bool SelfContained
        {
            get { return _selfContained; }
            set
            {
                if (SetProperty(ref _selfContained, value))
                {
                    UpdatePlan();
                }
            }
        }

        public bool SingleFile
        {
            get { return _singleFile; }
            set
            {
                if (SetProperty(ref _singleFile, value))
                {
                    UpdatePlan();
                }
            }
        }

        public string RuntimeIdentifier
        {
            get { return _runtimeIdentifier; }
            set
            {
                if (SetProperty(ref _runtimeIdentifier, value))
                {
                    UpdatePlan();
                }
            }
        }

        // ---------------------------------------------------------------- Piano di build

        public string ToolchainSummary
        {
            get { return _toolchainSummary; }
            private set { SetProperty(ref _toolchainSummary, value); }
        }

        public BuildPlan Plan
        {
            get { return _plan; }
            private set
            {
                if (SetProperty(ref _plan, value))
                {
                    OnPropertyChanged("PlanDescription");
                    OnPropertyChanged("CommandPreview");
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string PlanDescription
        {
            get { return Plan == null ? string.Empty : Plan.Description; }
        }

        public string CommandPreview
        {
            get { return Plan == null || !Plan.CanBuild ? string.Empty : Plan.CommandLineText; }
        }

        public ObservableCollection<PlanMessage> PlanMessages { get; private set; }

        // ---------------------------------------------------------------- Esecuzione

        public ObservableCollection<LogLine> Log { get; private set; }

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string StatusText
        {
            get { return _statusText; }
            private set { SetProperty(ref _statusText, value); }
        }

        public string LastExecutable
        {
            get { return _lastExecutable; }
            private set { SetProperty(ref _lastExecutable, value); }
        }

        // ---------------------------------------------------------------- Ciclo di vita

        /// <summary>Rilevamento degli strumenti di build (dotnet, MSBuild, targeting pack) in background.</summary>
        public async Task InitializeAsync()
        {
            ToolchainSummary = "Rilevamento degli strumenti di build in corso...";
            _toolchains = await Task.Run(() => _toolchainLocator.Locate());
            ToolchainSummary = DescribeToolchains(_toolchains);
            UpdatePlan();
        }

        private async Task RefreshAsync()
        {
            await InitializeAsync();
            LoadInput();
        }

        // ---------------------------------------------------------------- Logica

        private void LoadInput()
        {
            InputError = null;
            IsSolution = false;
            SolutionProjects.Clear();
            SelectedProject = null;

            var path = (InputPath ?? string.Empty).Trim().Trim('"');
            if (path.Length == 0)
            {
                return;
            }

            try
            {
                if (!File.Exists(path))
                {
                    InputError = "File non trovato.";
                    return;
                }

                if (SolutionParser.IsSolutionFile(path))
                {
                    LoadSolution(path);
                }
                else if (ProjectAnalyzer.IsProjectFile(path))
                {
                    SelectedProject = _analyzer.Analyze(path);
                }
                else
                {
                    InputError = "Selezionare un file .sln, .slnx, .csproj, .vbproj o .fsproj.";
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Xml.XmlException || ex is InvalidDataException)
            {
                InputError = "Impossibile leggere il file: " + ex.Message;
            }
        }

        private void LoadSolution(string path)
        {
            IsSolution = true;
            var skipped = 0;
            foreach (var project in _solutionParser.Parse(path))
            {
                try
                {
                    var info = _analyzer.Analyze(project.FullPath, solutionPath: path);
                    if (info.IsExecutable && !info.IsWebProject)
                    {
                        SolutionProjects.Add(info);
                    }
                    else
                    {
                        skipped++;
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Xml.XmlException || ex is InvalidDataException)
                {
                    AppendLog(new LogLine("Progetto ignorato (" + project.Name + "): " + ex.Message, LogKind.Warning));
                }
            }

            if (SolutionProjects.Count == 0)
            {
                InputError = "La solution non contiene progetti eseguibili (OutputType Exe/WinExe).";
                return;
            }

            StatusText = string.Format("{0} progetti eseguibili nella solution ({1} librerie/altro esclusi).", SolutionProjects.Count, skipped);
            SelectedProject = SolutionProjects[0];
        }

        private void OnProjectChanged()
        {
            TargetFrameworks.Clear();
            if (SelectedProject != null)
            {
                foreach (var tfm in SelectedProject.TargetFrameworks)
                {
                    TargetFrameworks.Add(tfm);
                }

                if (string.IsNullOrWhiteSpace(OutputDirectory))
                {
                    _outputDirectory = Path.Combine(SelectedProject.Directory, "BuildExe-output");
                    OnPropertyChanged("OutputDirectory");
                }
            }

            OnPropertyChanged("HasProject");
            OnPropertyChanged("IsMultiTarget");
            OnPropertyChanged("TargetFrameworksText");
            OnPropertyChanged("ResolvedOutputDirectory");

            _selectedTargetFramework = SelectedProject == null ? null : SelectedProject.DefaultTargetFramework;
            OnPropertyChanged("SelectedTargetFramework");
            OnPropertyChanged("IsNetCoreTarget");
            UpdatePlan();
        }

        private void UpdatePlan()
        {
            PlanMessages.Clear();
            if (SelectedProject == null)
            {
                Plan = null;
                return;
            }

            if (_toolchains == null)
            {
                Plan = null;
                PlanMessages.Add(new PlanMessage("Attendere il rilevamento degli strumenti di build...", false));
                return;
            }

            var plan = _planner.CreatePlan(SelectedProject, CreateOptions(), _toolchains);
            if (!string.IsNullOrWhiteSpace(OutputDirectory) && ResolvedOutputDirectory == null)
            {
                plan.Errors.Add("Il percorso della cartella di output non è valido.");
            }

            foreach (var error in plan.Errors)
            {
                PlanMessages.Add(new PlanMessage(error, true));
            }

            foreach (var warning in plan.Warnings)
            {
                PlanMessages.Add(new PlanMessage(warning, false));
            }

            Plan = plan;
        }

        private BuildOptions CreateOptions()
        {
            var isNetCore = IsNetCoreTarget;
            return new BuildOptions
            {
                OutputDirectory = ResolvedOutputDirectory,
                TargetFramework = SelectedTargetFramework,
                SolutionPath = IsSolution ? (InputPath ?? string.Empty).Trim().Trim('"') : null,
                SelfContained = isNetCore && SelfContained,
                SingleFile = isNetCore && SingleFile,
                RuntimeIdentifier = RuntimeIdentifier
            };
        }

        private async Task BuildAsync()
        {
            // Rilegge il progetto: potrebbe essere stato modificato dopo la selezione.
            var currentPath = SelectedProject.FullPath;
            LoadInputPreservingSelection(currentPath);
            if (Plan == null || !Plan.CanBuild)
            {
                StatusText = "Build non avviata: correggere gli errori indicati.";
                return;
            }

            var plan = Plan;
            var project = SelectedProject;
            var output = ResolvedOutputDirectory;

            Log.Clear();
            LastExecutable = null;
            IsBusy = true;
            StatusText = "Build in corso: " + project.Name + " (" + plan.TargetFramework.DisplayName + ")...";
            _cancellation = new CancellationTokenSource();

            try
            {
                var progress = new Progress<string>(line => AppendLog(LogLine.FromBuildOutput(line)));
                var result = await _buildService.RunAsync(plan, project, output, progress, _cancellation.Token);

                if (result.Succeeded)
                {
                    LastExecutable = result.Executables.FirstOrDefault();
                    StatusText = string.Format("Build completata in {0:0.0} s.", result.Duration.TotalSeconds);
                    AppendLog(new LogLine(string.Empty, LogKind.Normal));
                    AppendLog(new LogLine("Build completata. Eseguibile: " + (LastExecutable ?? "(nessun .exe trovato in " + output + ")"), LogKind.Success));
                }
                else
                {
                    StatusText = "Build fallita (exit code " + result.ExitCode + ").";
                    AppendLog(new LogLine(StatusText, LogKind.Error));
                }
            }
            catch (OperationCanceledException)
            {
                StatusText = "Build annullata.";
                AppendLog(new LogLine(StatusText, LogKind.Warning));
            }
            finally
            {
                _cancellation.Dispose();
                _cancellation = null;
                IsBusy = false;
            }
        }

        private void LoadInputPreservingSelection(string projectPath)
        {
            var output = OutputDirectory;
            var tfm = SelectedTargetFramework;
            LoadInput();

            if (IsSolution)
            {
                var match = SolutionProjects.FirstOrDefault(p => string.Equals(p.FullPath, projectPath, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    SelectedProject = match;
                }
            }

            OutputDirectory = output;
            if (tfm != null && TargetFrameworks.Contains(tfm))
            {
                SelectedTargetFramework = tfm;
            }

            UpdatePlan();
        }

        private void AppendLog(LogLine line)
        {
            if (Log.Count >= MaxLogLines)
            {
                Log.RemoveAt(0);
            }

            Log.Add(line);
        }

        private void BrowseInput()
        {
            var path = _dialogs.PickProjectOrSolution(InputPath);
            if (path != null)
            {
                InputPath = path;
            }
        }

        private void BrowseOutput()
        {
            var path = _dialogs.PickFolder(ResolvedOutputDirectory ?? (SelectedProject != null ? SelectedProject.Directory : null));
            if (path != null)
            {
                OutputDirectory = path;
            }
        }

        private void Cancel()
        {
            if (_cancellation != null)
            {
                StatusText = "Annullamento in corso...";
                _cancellation.Cancel();
            }
        }

        private void OpenOutput()
        {
            _dialogs.OpenFolder(ResolvedOutputDirectory);
        }

        private void CopyLog()
        {
            _dialogs.CopyToClipboard(string.Join(Environment.NewLine, Log.Select(l => l.Text)));
        }

        private void OnUnexpectedError(Exception ex)
        {
            IsBusy = false;
            StatusText = "Errore: " + ex.Message;
            try
            {
                AppendLog(new LogLine(ex.ToString(), LogKind.Error));
            }
            catch (InvalidOperationException)
            {
                // Il log non deve mai nascondere l'errore originale: lo mostriamo comunque sotto.
            }

            _dialogs.ShowError(ex.ToString());
        }

        private static string DescribeToolchains(Toolchains t)
        {
            var dotnet = t.DotNetSdks.Count > 0
                ? ".NET SDK: " + string.Join(", ", t.DotNetSdks)
                : ".NET SDK: non installato";
            var msbuild = t.MsBuildPath != null
                ? "MSBuild " + t.MsBuildVersion
                : "MSBuild: non trovato";
            var packs = t.InstalledTargetingPacks.Count > 0
                ? "Targeting pack .NET Framework: " + string.Join(", ", t.InstalledTargetingPacks.Select(TargetFramework.FormatVersion))
                : "Targeting pack .NET Framework: nessuno";
            return dotnet + "   |   " + msbuild + "   |   " + packs;
        }
    }
}
