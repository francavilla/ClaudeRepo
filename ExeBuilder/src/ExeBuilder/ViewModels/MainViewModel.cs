using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ExeBuilder.Core.Analysis;
using ExeBuilder.Core.Execution;
using ExeBuilder.Core.Model;
using ExeBuilder.Core.Planning;
using ExeBuilder.Core.Toolchain;
using ExeBuilder.Mvvm;
using ExeBuilder.Services;

namespace ExeBuilder.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        private const int MaxLogLines = 20000;
        private const string DefaultOutputFolder = "ExeBuilder-output";

        private readonly ProjectAnalyzer _analyzer;
        private readonly SolutionParser _solutionParser;
        private readonly BuildPlanner _planner;
        private readonly BuildService _buildService;
        private readonly IToolchainLocator _toolchainLocator;
        private readonly IDialogService _dialogs;

        private string _inputPath;
        private string _inputError;
        private bool _isSolution;
        private ProjectItemViewModel _currentProject;
        private string _outputDirectory;
        private bool _selfContained;
        private bool _singleFile;
        private string _runtimeIdentifier = "win-x64";
        private Toolchains _toolchains;
        private string _toolchainSummary = "Rilevamento degli strumenti di build in corso...";
        private bool _isBusy;
        private string _statusText = "Pronto.";
        private bool _suspendPlanUpdates;
        private BuildOutcome _outcome;
        private string _outcomeTitle;
        private string _outcomeDetail;
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

            Projects = new ObservableCollection<ProjectItemViewModel>();
            PlanMessages = new ObservableCollection<PlanMessage>();
            Log = new ObservableCollection<LogLine>();

            BrowseInputCommand = new RelayCommand(BrowseInput, () => !IsBusy);
            BrowseOutputCommand = new RelayCommand(BrowseOutput, () => !IsBusy);
            BuildCommand = new AsyncRelayCommand(BuildAsync, CanBuild, OnUnexpectedError);
            CancelCommand = new RelayCommand(Cancel, () => IsBusy);
            OpenOutputCommand = new RelayCommand(OpenOutput, () => ResolvedOutputDirectory != null && Directory.Exists(ResolvedOutputDirectory));
            CopyLogCommand = new RelayCommand(CopyLog, () => Log.Count > 0);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy, OnUnexpectedError);
            SelectAllCommand = new RelayCommand(() => SetSelection(true), () => !IsBusy && Projects.Any(p => !p.IsSelected));
            SelectNoneCommand = new RelayCommand(() => SetSelection(false), () => !IsBusy && Projects.Any(p => p.IsSelected));
        }

        // ---------------------------------------------------------------- Applicazione

        /// <summary>Versione dell'applicazione (da &lt;Version&gt; in Directory.Build.props).</summary>
        public string AppVersion
        {
            get { return AppInfo.Version; }
        }

        public string WindowTitle
        {
            get { return AppInfo.Name + " " + AppInfo.Version; }
        }

        // ---------------------------------------------------------------- Comandi

        public ICommand BrowseInputCommand { get; private set; }

        public ICommand BrowseOutputCommand { get; private set; }

        public ICommand BuildCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        public ICommand OpenOutputCommand { get; private set; }

        public ICommand CopyLogCommand { get; private set; }

        public ICommand RefreshCommand { get; private set; }

        public ICommand SelectAllCommand { get; private set; }

        public ICommand SelectNoneCommand { get; private set; }

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

        /// <summary>Progetti eseguibili: uno solo per un progetto, tutti quelli eseguibili per una solution.</summary>
        public ObservableCollection<ProjectItemViewModel> Projects { get; private set; }

        /// <summary>Progetto mostrato nel pannello di analisi (riga evidenziata nell'elenco).</summary>
        public ProjectItemViewModel CurrentProject
        {
            get { return _currentProject; }
            set
            {
                if (SetProperty(ref _currentProject, value))
                {
                    OnPropertyChanged("HasProject");
                    RefreshMessages();
                }
            }
        }

        public bool HasProject
        {
            get { return CurrentProject != null; }
        }

        public IList<ProjectItemViewModel> SelectedProjects
        {
            get { return Projects.Where(p => p.IsSelected).ToList(); }
        }

        public string SelectionSummary
        {
            get
            {
                var selected = Projects.Count(p => p.IsSelected);
                if (Projects.Count == 0)
                {
                    return string.Empty;
                }

                if (selected == 0)
                {
                    return string.Format("{0} progetti eseguibili: spuntare quelli da compilare (o \"Seleziona tutti\").", Projects.Count);
                }

                return string.Format("{0} di {1} progetti eseguibili selezionati.", selected, Projects.Count);
            }
        }

        public string BuildButtonText
        {
            get
            {
                var selected = Projects.Count(p => p.IsSelected);
                return selected > 1 ? "Crea " + selected + " eseguibili" : "Crea eseguibile";
            }
        }

        // ---------------------------------------------------------------- Output e opzioni

        /// <summary>
        /// Cartella di output; se relativa, è relativa alla cartella della solution o del progetto.
        /// Con più progetti selezionati ognuno va in una sottocartella con il proprio nome.
        /// </summary>
        public string OutputDirectory
        {
            get { return _outputDirectory; }
            set
            {
                if (SetProperty(ref _outputDirectory, value))
                {
                    OnPropertyChanged("ResolvedOutputDirectory");
                    UpdatePlans();
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
                    var baseDirectory = BaseDirectory;
                    if (!Path.IsPathRooted(path) && baseDirectory != null)
                    {
                        path = Path.Combine(baseDirectory, path);
                    }

                    return Path.GetFullPath(path);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
                {
                    return null;
                }
            }
        }

        public string OutputHint
        {
            get
            {
                var resolved = ResolvedOutputDirectory;
                if (resolved == null)
                {
                    return string.Empty;
                }

                return Projects.Count(p => p.IsSelected) > 1
                    ? "→ " + resolved + Path.DirectorySeparatorChar + "<NomeProgetto>   (una sottocartella per progetto)"
                    : "→ " + resolved;
            }
        }

        public bool SelfContained
        {
            get { return _selfContained; }
            set
            {
                if (SetProperty(ref _selfContained, value))
                {
                    UpdatePlans();
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
                    UpdatePlans();
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
                    UpdatePlans();
                }
            }
        }

        /// <summary>Abilita le opzioni self-contained/single-file se almeno un progetto selezionato è .NET 5+/Core.</summary>
        public bool IsAnyNetCoreTarget
        {
            get { return Projects.Any(p => p.IsSelected && p.IsNetCoreTarget); }
        }

        // ---------------------------------------------------------------- Strumenti e messaggi

        public string ToolchainSummary
        {
            get { return _toolchainSummary; }
            private set { SetProperty(ref _toolchainSummary, value); }
        }

        /// <summary>Errori, avvisi e note del progetto corrente.</summary>
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

        /// <summary>Esito dell'ultima build (banner sopra la console, sempre visibile).</summary>
        public BuildOutcome Outcome
        {
            get { return _outcome; }
            private set
            {
                if (SetProperty(ref _outcome, value))
                {
                    OnPropertyChanged("HasOutcome");
                }
            }
        }

        public bool HasOutcome
        {
            get { return Outcome != BuildOutcome.None; }
        }

        public string OutcomeTitle
        {
            get { return _outcomeTitle; }
            private set { SetProperty(ref _outcomeTitle, value); }
        }

        public string OutcomeDetail
        {
            get { return _outcomeDetail; }
            private set { SetProperty(ref _outcomeDetail, value); }
        }

        private string BaseDirectory
        {
            get
            {
                if (IsSolution)
                {
                    try
                    {
                        return Path.GetDirectoryName(Path.GetFullPath(CleanInputPath));
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
                    {
                        return null;
                    }
                }

                return CurrentProject != null ? CurrentProject.Info.Directory : null;
            }
        }

        private string CleanInputPath
        {
            get { return (InputPath ?? string.Empty).Trim().Trim('"'); }
        }

        // ---------------------------------------------------------------- Ciclo di vita

        /// <summary>Rilevamento degli strumenti di build (dotnet, MSBuild, targeting pack) in background.</summary>
        public async Task InitializeAsync()
        {
            ToolchainSummary = "Rilevamento degli strumenti di build in corso...";
            _toolchains = await Task.Run(() => _toolchainLocator.Locate());
            ToolchainSummary = DescribeToolchains(_toolchains);
            UpdatePlans();
        }

        private async Task RefreshAsync()
        {
            await InitializeAsync();
            ReloadPreservingSelection();
        }

        // ---------------------------------------------------------------- Caricamento

        private void LoadInput()
        {
            if (!IsBusy)
            {
                SetOutcome(BuildOutcome.None, null, null);
            }

            InputError = null;
            IsSolution = false;
            CurrentProject = null;
            Projects.Clear();

            var path = CleanInputPath;
            if (path.Length > 0)
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        InputError = "File non trovato.";
                    }
                    else if (SolutionParser.IsSolutionFile(path))
                    {
                        LoadSolution(path);
                    }
                    else if (ProjectAnalyzer.IsProjectFile(path))
                    {
                        var item = CreateItem(_analyzer.Analyze(path));
                        item.IsSelected = true;
                        Projects.Add(item);
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

            CurrentProject = Projects.FirstOrDefault();
            var baseDirectory = BaseDirectory;
            if (CurrentProject != null && baseDirectory != null && string.IsNullOrWhiteSpace(OutputDirectory))
            {
                _outputDirectory = Path.Combine(baseDirectory, DefaultOutputFolder);
                OnPropertyChanged("OutputDirectory");
            }

            OnPropertyChanged("ResolvedOutputDirectory");
            OnSelectionChanged();
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
                        Projects.Add(CreateItem(info));
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

            if (Projects.Count == 0)
            {
                InputError = "La solution non contiene progetti eseguibili (OutputType Exe/WinExe).";
                return;
            }

            // Un solo eseguibile: selezionato subito. Più di uno: decide l'utente (uno, alcuni o tutti).
            if (Projects.Count == 1)
            {
                Projects[0].IsSelected = true;
            }

            StatusText = string.Format(
                "Solution: {0} progetti eseguibili, {1} librerie/altro esclusi.",
                Projects.Count,
                skipped);
        }

        private ProjectItemViewModel CreateItem(ProjectInfo info)
        {
            return new ProjectItemViewModel(info, OnSelectionChanged);
        }

        /// <summary>Rilegge i file (potrebbero essere cambiati) mantenendo selezione, target e riga corrente.</summary>
        private void ReloadPreservingSelection()
        {
            var selected = new HashSet<string>(Projects.Where(p => p.IsSelected).Select(p => p.Info.FullPath), StringComparer.OrdinalIgnoreCase);
            var targets = Projects.ToDictionary(p => p.Info.FullPath, p => p.SelectedTargetFramework, StringComparer.OrdinalIgnoreCase);
            var current = CurrentProject != null ? CurrentProject.Info.FullPath : null;
            var output = OutputDirectory;

            _suspendPlanUpdates = true;
            try
            {
                LoadInput();
                foreach (var item in Projects)
                {
                    TargetFramework tfm;
                    if (targets.TryGetValue(item.Info.FullPath, out tfm) && tfm != null && item.TargetFrameworks.Contains(tfm))
                    {
                        item.SelectedTargetFramework = tfm;
                    }

                    if (IsSolution)
                    {
                        item.IsSelected = selected.Contains(item.Info.FullPath);
                    }
                }

                CurrentProject = Projects.FirstOrDefault(p => string.Equals(p.Info.FullPath, current, StringComparison.OrdinalIgnoreCase))
                    ?? Projects.FirstOrDefault();
                _outputDirectory = output;
                OnPropertyChanged("OutputDirectory");
                OnPropertyChanged("ResolvedOutputDirectory");
            }
            finally
            {
                _suspendPlanUpdates = false;
            }

            OnSelectionChanged();
        }

        // ---------------------------------------------------------------- Piani di build

        private void SetSelection(bool selected)
        {
            _suspendPlanUpdates = true;
            try
            {
                foreach (var item in Projects)
                {
                    item.IsSelected = selected;
                }
            }
            finally
            {
                _suspendPlanUpdates = false;
            }

            OnSelectionChanged();
        }

        private void OnSelectionChanged()
        {
            OnPropertyChanged("SelectionSummary");
            OnPropertyChanged("BuildButtonText");
            OnPropertyChanged("OutputHint");
            OnPropertyChanged("IsAnyNetCoreTarget");
            UpdatePlans();
        }

        private void UpdatePlans()
        {
            if (_suspendPlanUpdates)
            {
                return;
            }

            OnPropertyChanged("OutputHint");
            var selectedCount = Projects.Count(p => p.IsSelected);
            var resolvedOutput = ResolvedOutputDirectory;

            // Con più progetti viene svuotata la cartella radice: non deve contenere i sorgenti di nessuno.
            var rootProblem = selectedCount > 1 ? OutputFolder.ValidateForCleaning(resolvedOutput, ProtectedDirectories()) : null;

            foreach (var item in Projects)
            {
                if (_toolchains == null)
                {
                    item.Plan = null;
                    continue;
                }

                // Con più progetti ciascuno ha la sua sottocartella: niente DLL sovrascritte tra progetti diversi.
                var multiple = selectedCount > 1 || (!item.IsSelected && selectedCount > 0);
                item.OutputDirectory = resolvedOutput == null
                    ? null
                    : multiple ? Path.Combine(resolvedOutput, item.Name) : resolvedOutput;

                var plan = _planner.CreatePlan(item.Info, CreateOptions(item), _toolchains);
                if (!string.IsNullOrWhiteSpace(OutputDirectory) && resolvedOutput == null)
                {
                    plan.Errors.Add("Il percorso della cartella di output non è valido.");
                }

                if (rootProblem != null && item.IsSelected && !plan.Errors.Contains(rootProblem))
                {
                    plan.Errors.Add(rootProblem);
                }

                item.Plan = plan;
            }

            RefreshMessages();
            CommandManager.InvalidateRequerySuggested();
        }

        private IEnumerable<string> ProtectedDirectories()
        {
            var directories = Projects.Where(p => p.IsSelected).Select(p => p.Info.Directory).ToList();
            if (IsSolution)
            {
                directories.Add(BaseDirectory);
            }

            directories.AddRange(Projects.Where(p => p.IsSelected && p.Info.InferredSolutionDir != null).Select(p => p.Info.InferredSolutionDir));
            return directories;
        }

        private BuildOptions CreateOptions(ProjectItemViewModel item)
        {
            return new BuildOptions
            {
                OutputDirectory = item.OutputDirectory,
                TargetFramework = item.SelectedTargetFramework,
                SolutionPath = IsSolution ? CleanInputPath : null,
                SelfContained = item.IsNetCoreTarget && SelfContained,
                SingleFile = item.IsNetCoreTarget && SingleFile,
                RuntimeIdentifier = RuntimeIdentifier
            };
        }

        private void RefreshMessages()
        {
            PlanMessages.Clear();
            if (CurrentProject == null)
            {
                return;
            }

            if (_toolchains == null)
            {
                PlanMessages.Add(new PlanMessage("Attendere il rilevamento degli strumenti di build...", PlanMessageKind.Info));
                return;
            }

            var plan = CurrentProject.Plan;
            if (plan == null)
            {
                return;
            }

            foreach (var error in plan.Errors)
            {
                PlanMessages.Add(new PlanMessage(error, PlanMessageKind.Error));
            }

            foreach (var warning in plan.Warnings)
            {
                PlanMessages.Add(new PlanMessage(warning, PlanMessageKind.Warning));
            }

            foreach (var note in plan.Notes)
            {
                PlanMessages.Add(new PlanMessage(note, PlanMessageKind.Info));
            }

            var otherInvalid = Projects.Where(p => p.IsSelected && p != CurrentProject && !p.CanBuild).Select(p => p.Name).ToList();
            if (otherInvalid.Count > 0)
            {
                PlanMessages.Add(new PlanMessage(
                    "Altri progetti selezionati non compilabili: " + string.Join(", ", otherInvalid) + " (selezionarli nell'elenco per i dettagli).",
                    PlanMessageKind.Error));
            }
        }

        private bool CanBuild()
        {
            var selected = Projects.Where(p => p.IsSelected).ToList();
            return !IsBusy && _toolchains != null && selected.Count > 0 && selected.All(p => p.CanBuild);
        }

        // ---------------------------------------------------------------- Build

        private async Task BuildAsync()
        {
            // Rilegge i progetti: potrebbero essere stati modificati dopo la selezione.
            ReloadPreservingSelection();
            if (!CanBuild())
            {
                StatusText = "Build non avviata: correggere gli errori indicati.";
                return;
            }

            var targets = SelectedProjects;
            foreach (var item in Projects)
            {
                item.RunStatus = item.IsSelected ? "in attesa" : null;
            }

            Log.Clear();
            IsBusy = true;
            SetOutcome(BuildOutcome.Running, "Build in corso...", null);
            _cancellation = new CancellationTokenSource();
            var startedAt = DateTime.Now;
            var succeeded = new List<ProjectItemViewModel>();
            var failed = new List<ProjectItemViewModel>();
            var executables = new List<string>();

            try
            {
                if (!await CleanOutputAsync(ResolvedOutputDirectory, targets))
                {
                    return;
                }

                for (var i = 0; i < targets.Count; i++)
                {
                    var item = targets[i];
                    CurrentProject = item;
                    item.RunStatus = "in corso...";
                    StatusText = string.Format("Build {0}/{1}: {2} ({3})...", i + 1, targets.Count, item.Name, item.Plan.TargetFramework.DisplayName);
                    SetOutcome(
                        BuildOutcome.Running,
                        targets.Count > 1 ? string.Format("Build in corso {0}/{1}: {2}", i + 1, targets.Count, item.Name) : "Build in corso: " + item.Name,
                        item.Plan.TargetFramework.DisplayName + " → " + item.OutputDirectory);

                    if (targets.Count > 1)
                    {
                        AppendLog(new LogLine(string.Format("════ [{0}/{1}] {2} ════", i + 1, targets.Count, item.Name), LogKind.Command));
                    }

                    var progress = new Progress<string>(line => AppendLog(LogLine.FromBuildOutput(line)));
                    BuildResult result;
                    try
                    {
                        result = await _buildService.RunAsync(item.Plan, item.Info, item.OutputDirectory, progress, _cancellation.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        item.RunStatus = "annullata";
                        foreach (var pending in targets.Skip(i + 1))
                        {
                            pending.RunStatus = "non eseguita";
                        }

                        throw;
                    }

                    if (result.Succeeded)
                    {
                        succeeded.Add(item);
                        var exe = result.Executables.FirstOrDefault();
                        if (exe != null)
                        {
                            executables.Add(exe);
                        }

                        item.RunStatus = string.Format("✔ riuscita ({0:0.0} s)", result.Duration.TotalSeconds);
                        AppendLog(new LogLine("✔ " + item.Name + ": " + (exe ?? "nessun .exe trovato in " + item.OutputDirectory), LogKind.Success));
                    }
                    else
                    {
                        failed.Add(item);
                        item.RunStatus = "✖ fallita (exit code " + result.ExitCode + ")";
                        AppendLog(new LogLine("✖ " + item.Name + ": build fallita (exit code " + result.ExitCode + ").", LogKind.Error));
                    }

                    AppendLog(new LogLine(string.Empty, LogKind.Normal));
                }

                StatusText = failed.Count == 0
                    ? (succeeded.Count == 1 ? "Build completata." : string.Format("Tutte le {0} build completate.", succeeded.Count))
                    : string.Format("{0} riuscite, {1} fallite: {2}.", succeeded.Count, failed.Count, string.Join(", ", failed.Select(f => f.Name)));

                var totalSeconds = targets.Count == 0 ? 0 : (DateTime.Now - startedAt).TotalSeconds;
                if (failed.Count == 0)
                {
                    SetOutcome(
                        BuildOutcome.Succeeded,
                        succeeded.Count == 1 ? "Build riuscita" : string.Format("Tutte le {0} build riuscite", succeeded.Count),
                        (executables.Count == 0 ? "Nessun .exe trovato in " + ResolvedOutputDirectory
                            : executables.Count == 1 ? executables[0]
                            : string.Format("{0} eseguibili in {1}", executables.Count, ResolvedOutputDirectory))
                            + string.Format("  ·  {0:0.0} s", totalSeconds));
                }
                else
                {
                    SetOutcome(
                        BuildOutcome.Failed,
                        succeeded.Count == 0 ? "Build fallita" : string.Format("{0} riuscite, {1} fallite", succeeded.Count, failed.Count),
                        "Non riuscite: " + string.Join(", ", failed.Select(f => f.Name)) + ". Le righe in rosso nel log indicano gli errori.");
                }

                if (targets.Count > 1)
                {
                    AppendLog(new LogLine("Riepilogo: " + StatusText, failed.Count == 0 ? LogKind.Success : LogKind.Error));
                    foreach (var exe in executables)
                    {
                        AppendLog(new LogLine("   " + exe, LogKind.Success));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                StatusText = "Build annullata.";
                AppendLog(new LogLine(StatusText, LogKind.Warning));
                SetOutcome(BuildOutcome.Cancelled, "Build annullata", "La build è stata interrotta: l'output potrebbe essere incompleto.");
            }
            finally
            {
                _cancellation.Dispose();
                _cancellation = null;
                IsBusy = false;
            }
        }

        /// <summary>
        /// Svuota la cartella di output prima della build, così contiene solo il risultato di questa build.
        /// </summary>
        private async Task<bool> CleanOutputAsync(string directory, IList<ProjectItemViewModel> targets)
        {
            if (!Directory.Exists(directory))
            {
                return true;
            }

            AppendLog(new LogLine("Pulizia della cartella di output " + directory + " ...", LogKind.Command));
            try
            {
                var deleted = await Task.Run(() => OutputFolder.Clean(directory));
                AppendLog(new LogLine(deleted + " file eliminati.", LogKind.Normal));
                AppendLog(new LogLine(string.Empty, LogKind.Normal));
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
            {
                StatusText = "Impossibile svuotare la cartella di output: build non avviata.";
                SetOutcome(BuildOutcome.Failed, "Build non avviata", "Impossibile svuotare la cartella di output: un file è in uso (programma aperto o Esplora risorse?).");
                AppendLog(new LogLine(StatusText, LogKind.Error));
                AppendLog(new LogLine(ex.Message, LogKind.Error));
                AppendLog(new LogLine("Probabilmente un file è in uso: chiudere il programma avviato da quella cartella (o Esplora risorse) e riprovare.", LogKind.Warning));
                foreach (var item in targets)
                {
                    item.RunStatus = "non eseguita";
                }

                return false;
            }
        }

        // ---------------------------------------------------------------- Varie

        private void SetOutcome(BuildOutcome outcome, string title, string detail)
        {
            OutcomeTitle = title;
            OutcomeDetail = detail;
            Outcome = outcome;
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
            var path = _dialogs.PickFolder(ResolvedOutputDirectory ?? BaseDirectory);
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
            SetOutcome(BuildOutcome.Failed, "Errore imprevisto", ex.Message);
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
