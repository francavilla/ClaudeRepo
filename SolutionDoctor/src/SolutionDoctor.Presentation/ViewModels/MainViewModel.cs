using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Model;
using SolutionDoctor.Core.Reporting;
using SolutionDoctor.Presentation.Mvvm;
using SolutionDoctor.Presentation.Services;

namespace SolutionDoctor.Presentation.ViewModels
{
    /// <summary>
    /// ViewModel della finestra principale: sceglie cosa analizzare, lancia l'analisi (annullabile),
    /// mostra backlog, problemi, progetti e ordine di migrazione, e salva o copia il report.
    /// Non dipende da WPF: ogni interazione con il sistema passa dai servizi iniettati.
    /// </summary>
    public sealed class MainViewModel : ObservableObject
    {
        private static readonly int[] MonthsChoices = { 3, 6, 12, 24, 36 };

        private readonly IAnalyzer _analyzer;
        private readonly IDialogService _dialogs;
        private readonly ISettingsStore _settings;
        private readonly string _appVersion;

        private readonly RelayCommand _browseFileCommand;
        private readonly RelayCommand _browseFolderCommand;
        private readonly AsyncRelayCommand _analyzeCommand;
        private readonly RelayCommand _cancelCommand;
        private readonly RelayCommand _saveReportCommand;
        private readonly RelayCommand _copyReportCommand;
        private readonly RelayCommand _openSolutionFolderCommand;
        private readonly RelayCommand _openFindingCommand;
        private readonly RelayCommand _revealFindingCommand;
        private readonly RelayCommand _copyFindingPathCommand;
        private readonly RelayCommand _openBacklogFileCommand;
        private readonly RelayCommand _clearFiltersCommand;

        private CancellationTokenSource _cancellation;
        private AnalysisReport _report;
        private AnalysisOptions _lastOptions;
        private List<FindingRow> _allFindings = new List<FindingRow>();
        private bool _suspendFilters;

        private string _inputPath = string.Empty;
        private string _inputError;
        private bool _useGit = true;
        private int _gitMonths = 12;
        private bool _isBusy;
        private string _statusText = "Scegli una solution, un progetto o una cartella da analizzare.";
        private string _analysisError;
        private bool _hasResult;
        private ResultTab _selectedTab = ResultTab.Backlog;

        private IReadOnlyList<BacklogRow> _backlog = new List<BacklogRow>();
        private IReadOnlyList<FindingRow> _findings = new List<FindingRow>();
        private IReadOnlyList<ProjectRow> _projects = new List<ProjectRow>();
        private IReadOnlyList<MigrationStepRow> _migrationSteps = new List<MigrationStepRow>();
        private IReadOnlyList<RuleFilterOption> _ruleFilters = new List<RuleFilterOption>();
        private BacklogRow _selectedBacklogItem;
        private IReadOnlyList<FindingRow> _selectedBacklogFindings = new List<FindingRow>();
        private FindingRow _selectedFinding;
        private SeverityFilterOption _selectedSeverityFilter;
        private RuleFilterOption _selectedRuleFilter;
        private string _searchText = string.Empty;

        public MainViewModel(IAnalyzer analyzer, IDialogService dialogs, ISettingsStore settings, string appVersion)
        {
            _analyzer = analyzer;
            _dialogs = dialogs;
            _settings = settings;
            _appVersion = appVersion;

            SeverityFilters = new List<SeverityFilterOption>
            {
                new SeverityFilterOption("Tutte le gravità", Severity.Info),
                new SeverityFilterOption("Almeno bassa", Severity.Low),
                new SeverityFilterOption("Almeno media", Severity.Medium),
                new SeverityFilterOption("Solo alta", Severity.High)
            };
            _selectedSeverityFilter = SeverityFilters[0];
            _selectedRuleFilter = AllRulesOption;
            _ruleFilters = new List<RuleFilterOption> { AllRulesOption };

            _browseFileCommand = new RelayCommand(BrowseFile, () => !IsBusy);
            _browseFolderCommand = new RelayCommand(BrowseFolder, () => !IsBusy);
            _analyzeCommand = new AsyncRelayCommand(AnalyzeAsync, () => CanAnalyze, OnUnexpectedError);
            _cancelCommand = new RelayCommand(Cancel, () => IsBusy);
            _saveReportCommand = new RelayCommand(SaveReport, () => HasResult && !IsBusy);
            _copyReportCommand = new RelayCommand(CopyReport, () => HasResult);
            _openSolutionFolderCommand = new RelayCommand(OpenSolutionFolder, () => HasResult);
            _openFindingCommand = new RelayCommand(() => _dialogs.OpenFile(_selectedFinding.FullPath), () => _selectedFinding != null);
            _revealFindingCommand = new RelayCommand(() => _dialogs.RevealInFolder(_selectedFinding.FullPath), () => _selectedFinding != null);
            _copyFindingPathCommand = new RelayCommand(() => _dialogs.CopyToClipboard(_selectedFinding.FullPath), () => _selectedFinding != null);
            _openBacklogFileCommand = new RelayCommand(() => _dialogs.OpenFile(_selectedBacklogItem.FullPath), () => _selectedBacklogItem != null);
            _clearFiltersCommand = new RelayCommand(ClearFilters, () => HasActiveFilters);
        }

        // ------------------------------------------------------------------ Intestazione

        public string AppVersion
        {
            get { return _appVersion; }
        }

        public string WindowTitle
        {
            get { return "SolutionDoctor" + (HasResult ? " — " + SolutionName : string.Empty); }
        }

        // ------------------------------------------------------------------ Input

        public string InputPath
        {
            get { return _inputPath; }
            set
            {
                if (SetProperty(ref _inputPath, value ?? string.Empty))
                {
                    InputError = ValidateInput(_inputPath);
                    RefreshCommands();
                }
            }
        }

        /// <summary>Problema del percorso scelto (non esiste, estensione non valida); nullo se va bene o è vuoto.</summary>
        public string InputError
        {
            get { return _inputError; }
            private set
            {
                if (SetProperty(ref _inputError, value))
                {
                    OnPropertyChanged(nameof(HasInputError));
                }
            }
        }

        public bool HasInputError
        {
            get { return !string.IsNullOrEmpty(_inputError); }
        }

        public bool UseGit
        {
            get { return _useGit; }
            set { SetProperty(ref _useGit, value); }
        }

        public IReadOnlyList<int> GitMonthsChoices
        {
            get { return MonthsChoices; }
        }

        public int GitMonths
        {
            get { return _gitMonths; }
            set { SetProperty(ref _gitMonths, MonthsChoices.Contains(value) ? value : 12); }
        }

        public bool CanAnalyze
        {
            get { return !IsBusy && !string.IsNullOrWhiteSpace(_inputPath) && string.IsNullOrEmpty(_inputError); }
        }

        // ------------------------------------------------------------------ Stato

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsIdle));
                    OnPropertyChanged(nameof(ShowEmptyState));
                    OnPropertyChanged(nameof(ShowBusyPlaceholder));
                    RefreshCommands();
                }
            }
        }

        public bool IsIdle
        {
            get { return !_isBusy; }
        }

        public string StatusText
        {
            get { return _statusText; }
            private set { SetProperty(ref _statusText, value); }
        }

        /// <summary>Errore dell'ultima analisi (solution illeggibile, accesso negato…); nullo se è andata bene.</summary>
        public string AnalysisError
        {
            get { return _analysisError; }
            private set
            {
                if (SetProperty(ref _analysisError, value))
                {
                    OnPropertyChanged(nameof(HasAnalysisError));
                }
            }
        }

        public bool HasAnalysisError
        {
            get { return !string.IsNullOrEmpty(_analysisError); }
        }

        public bool HasResult
        {
            get { return _hasResult; }
            private set
            {
                if (SetProperty(ref _hasResult, value))
                {
                    OnPropertyChanged(nameof(ShowEmptyState));
                    OnPropertyChanged(nameof(ShowBusyPlaceholder));
                    OnPropertyChanged(nameof(WindowTitle));
                    RefreshCommands();
                }
            }
        }

        /// <summary>Vero durante la prima analisi, quando non c'è ancora nulla da mostrare.</summary>
        public bool ShowBusyPlaceholder
        {
            get { return _isBusy && !_hasResult; }
        }

        /// <summary>Vero prima della prima analisi: mostra la presentazione dello strumento.</summary>
        public bool ShowEmptyState
        {
            get { return !_hasResult && !_isBusy; }
        }

        // ------------------------------------------------------------------ Riepilogo

        public string SolutionName
        {
            get { return _report != null ? _report.Solution.Name : string.Empty; }
        }

        public string SolutionRoot
        {
            get { return _report != null ? _report.Solution.RootDirectory : string.Empty; }
        }

        public int ProjectCount
        {
            get { return _report != null ? _report.Solution.Projects.Count : 0; }
        }

        public int WinFormsProjectCount
        {
            get { return _report != null ? _report.Solution.Projects.Count(p => p.IsWinForms) : 0; }
        }

        public int FormCount
        {
            get { return _report != null ? _report.UiClasses.Count : 0; }
        }

        public int TotalFindingCount
        {
            get { return _allFindings.Count; }
        }

        public int HighCount
        {
            get { return CountOf(Severity.High); }
        }

        public int MediumCount
        {
            get { return CountOf(Severity.Medium); }
        }

        public int LowCount
        {
            get { return CountOf(Severity.Low); }
        }

        public int InfoCount
        {
            get { return CountOf(Severity.Info); }
        }

        /// <summary>Come è stata calcolata la priorità (con o senza cronologia git).</summary>
        public string ChurnNote
        {
            get
            {
                if (_report == null || _lastOptions == null)
                {
                    return string.Empty;
                }

                if (_report.ChurnAvailable)
                {
                    return "Priorità = problemi × frequenza di modifica (commit degli ultimi " + _lastOptions.GitMonths + " mesi).";
                }

                return _lastOptions.UseGit
                    ? "Cronologia git non disponibile: la priorità si basa solo sui problemi."
                    : "Cronologia git non usata: la priorità si basa solo sui problemi.";
            }
        }

        public bool HasWarnings
        {
            get { return _report != null && _report.Solution.Warnings.Count > 0; }
        }

        public string WarningsText
        {
            get { return _report != null ? string.Join(Environment.NewLine, _report.Solution.Warnings) : string.Empty; }
        }

        // ------------------------------------------------------------------ Sezioni

        public ResultTab SelectedTab
        {
            get { return _selectedTab; }
            set
            {
                if (SetProperty(ref _selectedTab, value))
                {
                    OnPropertyChanged(nameof(IsBacklogTab));
                    OnPropertyChanged(nameof(IsFindingsTab));
                    OnPropertyChanged(nameof(IsProjectsTab));
                    OnPropertyChanged(nameof(IsMigrationTab));
                }
            }
        }

        // Una proprietà booleana per sezione: i RadioButton della barra delle sezioni si legano a queste
        // (il valore false, scritto da WPF quando si sceglie un'altra sezione, viene ignorato).
        public bool IsBacklogTab
        {
            get { return _selectedTab == ResultTab.Backlog; }
            set { if (value) { SelectedTab = ResultTab.Backlog; } }
        }

        public bool IsFindingsTab
        {
            get { return _selectedTab == ResultTab.Findings; }
            set { if (value) { SelectedTab = ResultTab.Findings; } }
        }

        public bool IsProjectsTab
        {
            get { return _selectedTab == ResultTab.Projects; }
            set { if (value) { SelectedTab = ResultTab.Projects; } }
        }

        public bool IsMigrationTab
        {
            get { return _selectedTab == ResultTab.Migration; }
            set { if (value) { SelectedTab = ResultTab.Migration; } }
        }

        public string FindingsTabTitle
        {
            get { return "Problemi (" + _allFindings.Count + ")"; }
        }

        public string ProjectsTabTitle
        {
            get { return "Progetti (" + _projects.Count + ")"; }
        }

        public IReadOnlyList<BacklogRow> Backlog
        {
            get { return _backlog; }
            private set { SetProperty(ref _backlog, value); }
        }

        public bool HasBacklog
        {
            get { return _backlog.Count > 0; }
        }

        public bool IsBacklogEmpty
        {
            get { return _backlog.Count == 0; }
        }

        public bool HasSelectedBacklogItem
        {
            get { return _selectedBacklogItem != null; }
        }

        public BacklogRow SelectedBacklogItem
        {
            get { return _selectedBacklogItem; }
            set
            {
                if (SetProperty(ref _selectedBacklogItem, value))
                {
                    SelectedBacklogFindings = value == null
                        ? new List<FindingRow>()
                        : _allFindings.Where(f => f.Project == value.Project && f.Subject == value.Subject).ToList();
                    OnPropertyChanged(nameof(HasSelectedBacklogItem));
                    RefreshCommands();
                }
            }
        }

        /// <summary>I problemi della classe selezionata nel backlog.</summary>
        public IReadOnlyList<FindingRow> SelectedBacklogFindings
        {
            get { return _selectedBacklogFindings; }
            private set { SetProperty(ref _selectedBacklogFindings, value); }
        }

        public IReadOnlyList<FindingRow> Findings
        {
            get { return _findings; }
            private set { SetProperty(ref _findings, value); }
        }

        public FindingRow SelectedFinding
        {
            get { return _selectedFinding; }
            set
            {
                if (SetProperty(ref _selectedFinding, value))
                {
                    OnPropertyChanged(nameof(HasSelectedFinding));
                    RefreshCommands();
                }
            }
        }

        public bool HasSelectedFinding
        {
            get { return _selectedFinding != null; }
        }

        public IReadOnlyList<ProjectRow> Projects
        {
            get { return _projects; }
            private set { SetProperty(ref _projects, value); }
        }

        public IReadOnlyList<MigrationStepRow> MigrationSteps
        {
            get { return _migrationSteps; }
            private set { SetProperty(ref _migrationSteps, value); }
        }

        /// <summary>Avviso sui riferimenti circolari tra progetti; nullo se non ce ne sono.</summary>
        public string CycleWarning
        {
            get
            {
                if (_report == null || _report.CycleMembers.Count == 0)
                {
                    return null;
                }

                return "Riferimenti circolari tra: " + string.Join(", ", _report.CycleMembers.Select(p => p.Name))
                    + ". Questi progetti non si possono ordinare: rompere il ciclo prima di migrare.";
            }
        }

        public bool HasCycleWarning
        {
            get { return CycleWarning != null; }
        }

        // ------------------------------------------------------------------ Filtri dei problemi

        public IReadOnlyList<SeverityFilterOption> SeverityFilters { get; private set; }

        public SeverityFilterOption SelectedSeverityFilter
        {
            get { return _selectedSeverityFilter; }
            set
            {
                if (value != null && SetProperty(ref _selectedSeverityFilter, value))
                {
                    ApplyFindingFilters();
                }
            }
        }

        public IReadOnlyList<RuleFilterOption> RuleFilters
        {
            get { return _ruleFilters; }
            private set { SetProperty(ref _ruleFilters, value); }
        }

        public RuleFilterOption SelectedRuleFilter
        {
            get { return _selectedRuleFilter; }
            set
            {
                if (value != null && SetProperty(ref _selectedRuleFilter, value))
                {
                    ApplyFindingFilters();
                }
            }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetProperty(ref _searchText, value ?? string.Empty))
                {
                    ApplyFindingFilters();
                }
            }
        }

        public bool HasActiveFilters
        {
            get
            {
                return _selectedSeverityFilter != SeverityFilters[0]
                    || _selectedRuleFilter.RuleId != null
                    || _searchText.Length > 0;
            }
        }

        /// <summary>"12 di 40 problemi".</summary>
        public string FilteredCountText
        {
            get { return _findings.Count + " di " + _allFindings.Count + (_allFindings.Count == 1 ? " problema" : " problemi"); }
        }

        public bool NoFindingsMatch
        {
            get { return _allFindings.Count > 0 && _findings.Count == 0; }
        }

        // ------------------------------------------------------------------ Comandi

        public ICommand BrowseFileCommand { get { return _browseFileCommand; } }

        public ICommand BrowseFolderCommand { get { return _browseFolderCommand; } }

        public ICommand AnalyzeCommand { get { return _analyzeCommand; } }

        public ICommand CancelCommand { get { return _cancelCommand; } }

        public ICommand SaveReportCommand { get { return _saveReportCommand; } }

        public ICommand CopyReportCommand { get { return _copyReportCommand; } }

        public ICommand OpenSolutionFolderCommand { get { return _openSolutionFolderCommand; } }

        public ICommand OpenFindingCommand { get { return _openFindingCommand; } }

        public ICommand RevealFindingCommand { get { return _revealFindingCommand; } }

        public ICommand CopyFindingPathCommand { get { return _copyFindingPathCommand; } }

        public ICommand OpenBacklogFileCommand { get { return _openBacklogFileCommand; } }

        public ICommand ClearFiltersCommand { get { return _clearFiltersCommand; } }

        // ------------------------------------------------------------------ Avvio

        /// <summary>Ripristina le ultime preferenze (percorso, cronologia git).</summary>
        public void LoadSettings()
        {
            var settings = _settings.Load();
            UseGit = settings.UseGit;
            GitMonths = settings.GitMonths;
            if (!string.IsNullOrWhiteSpace(settings.LastPath) && (File.Exists(settings.LastPath) || Directory.Exists(settings.LastPath)))
            {
                InputPath = settings.LastPath;
            }
        }

        // ------------------------------------------------------------------ Analisi

        /// <summary>Esegue l'analisi del percorso scelto. Pubblico per poterlo attendere nei test.</summary>
        public async Task AnalyzeAsync()
        {
            if (!CanAnalyze)
            {
                return;
            }

            var path = NormalizePath(_inputPath);
            var options = new AnalysisOptions { UseGit = _useGit, GitMonths = _gitMonths };

            AnalysisError = null;
            IsBusy = true;
            StatusText = "Analisi in corso…";
            _cancellation = new CancellationTokenSource();
            var token = _cancellation.Token;
            var progress = new Progress<string>(message => { if (_isBusy) { StatusText = message; } });

            try
            {
                var report = await _analyzer.AnalyzeAsync(path, options, progress, token);
                token.ThrowIfCancellationRequested();

                _lastOptions = options;
                ShowReport(report);
                SaveSettings(path);
                StatusText = "Analisi completata: " + report.Solution.Projects.Count + " progetti, "
                    + report.UiClasses.Count + " form, " + report.Findings.Count + " problemi.";
            }
            catch (OperationCanceledException)
            {
                StatusText = "Analisi annullata.";
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                AnalysisError = ex.Message;
                StatusText = "Analisi non riuscita.";
            }
            finally
            {
                _cancellation.Dispose();
                _cancellation = null;
                IsBusy = false;
            }
        }

        private void Cancel()
        {
            var cancellation = _cancellation;
            if (cancellation != null)
            {
                StatusText = "Annullamento in corso…";
                cancellation.Cancel();
            }
        }

        private void ShowReport(AnalysisReport report)
        {
            _report = report;
            var root = report.Solution.RootDirectory;
            _allFindings = report.Findings.Select(f => new FindingRow(f, root)).ToList();

            var issuesByProject = report.Findings
                .GroupBy(f => f.Project ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.Count());
            Projects = report.Solution.Projects
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Select(p => new ProjectRow(p, issuesByProject.ContainsKey(p.Name) ? issuesByProject[p.Name] : 0))
                .ToList();

            var byPath = report.Solution.Projects.ToDictionary(p => p.Path, p => p, StringComparer.OrdinalIgnoreCase);
            MigrationSteps = report.MigrationOrder
                .Select((p, i) => new MigrationStepRow(
                    i + 1,
                    p,
                    p.ProjectReferences.Where(byPath.ContainsKey).Select(r => byPath[r]).Where(d => d != p).Distinct()))
                .ToList();

            Backlog = report.Backlog.Select((item, i) => new BacklogRow(i + 1, item, root)).ToList();

            _suspendFilters = true;
            try
            {
                RuleFilters = new[] { AllRulesOption }
                    .Concat(report.Findings
                        .Select(f => f.RuleId)
                        .Distinct()
                        .OrderBy(id => id, StringComparer.Ordinal)
                        .Select(id =>
                        {
                            var rule = RuleCatalog.Find(id);
                            return new RuleFilterOption(id, id + " — " + (rule != null ? rule.Title : id));
                        }))
                    .ToList();
                _selectedSeverityFilter = SeverityFilters[0];
                _selectedRuleFilter = RuleFilters[0];
                _searchText = string.Empty;
            }
            finally
            {
                _suspendFilters = false;
            }

            _selectedFinding = null;
            SelectedBacklogItem = null;
            SelectedBacklogItem = Backlog.FirstOrDefault();
            ApplyFindingFilters();
            SelectedTab = ResultTab.Backlog;
            HasResult = true;

            // Cambiano quasi tutte le proprietà di riepilogo: WPF le rilegge tutte.
            OnPropertyChanged(string.Empty);
            RefreshCommands();
        }

        // ------------------------------------------------------------------ Filtri

        private void ApplyFindingFilters()
        {
            if (_suspendFilters)
            {
                return;
            }

            var minimum = _selectedSeverityFilter.Minimum;
            var ruleId = _selectedRuleFilter.RuleId;
            var text = _searchText.Trim();

            Findings = _allFindings
                .Where(f => f.Severity >= minimum)
                .Where(f => ruleId == null || f.RuleId == ruleId)
                .Where(f => text.Length == 0 || Matches(f, text))
                .ToList();

            if (_selectedFinding != null && !_findings.Contains(_selectedFinding))
            {
                SelectedFinding = null;
            }

            OnPropertyChanged(nameof(FilteredCountText));
            OnPropertyChanged(nameof(NoFindingsMatch));
            OnPropertyChanged(nameof(HasActiveFilters));
            OnPropertyChanged(nameof(FindingsTabTitle));
            RefreshCommands();
        }

        private static bool Matches(FindingRow finding, string text)
        {
            return Contains(finding.RuleId, text) || Contains(finding.Message, text)
                || Contains(finding.FilePath, text) || Contains(finding.Subject, text);
        }

        private static bool Contains(string source, string text)
        {
            return source.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ClearFilters()
        {
            _suspendFilters = true;
            try
            {
                SelectedSeverityFilter = SeverityFilters[0];
                SelectedRuleFilter = RuleFilters.Count > 0 ? RuleFilters[0] : AllRulesOption;
                SearchText = string.Empty;
            }
            finally
            {
                _suspendFilters = false;
            }

            ApplyFindingFilters();
        }

        private static RuleFilterOption AllRulesOption { get; } = new RuleFilterOption(null, "Tutte le regole");

        // ------------------------------------------------------------------ Azioni

        private void BrowseFile()
        {
            var picked = _dialogs.PickSolutionOrProject(NormalizePath(_inputPath));
            if (!string.IsNullOrEmpty(picked))
            {
                InputPath = picked;
            }
        }

        private void BrowseFolder()
        {
            var picked = _dialogs.PickFolder(NormalizePath(_inputPath));
            if (!string.IsNullOrEmpty(picked))
            {
                InputPath = picked;
            }
        }

        private void SaveReport()
        {
            var path = _dialogs.PickReportFile(SuggestedReportFileName());
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                File.WriteAllText(path, MarkdownReportWriter.Write(_report), new UTF8Encoding(false));
                StatusText = "Report salvato in " + path;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _dialogs.ShowError("Impossibile salvare il report: " + ex.Message);
            }
        }

        private void CopyReport()
        {
            _dialogs.CopyToClipboard(MarkdownReportWriter.Write(_report));
            StatusText = "Report copiato negli appunti.";
        }

        private void OpenSolutionFolder()
        {
            _dialogs.OpenFolder(_report.Solution.RootDirectory);
        }

        private string SuggestedReportFileName()
        {
            var name = new string(_report.Solution.Name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
            return "SolutionDoctor-" + name + "-" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".md";
        }

        // ------------------------------------------------------------------ Supporto

        private void SaveSettings(string path)
        {
            _settings.Save(new AppSettings { LastPath = path, UseGit = _useGit, GitMonths = _gitMonths });
        }

        private void OnUnexpectedError(Exception exception)
        {
            AnalysisError = exception.Message;
            StatusText = "Errore imprevisto.";
            _dialogs.ShowError(exception.Message);
        }

        private int CountOf(Severity severity)
        {
            return _allFindings.Count(f => f.Severity == severity);
        }

        /// <summary>Toglie spazi e virgolette (tipiche di un percorso incollato da Esplora risorse).</summary>
        internal static string NormalizePath(string input)
        {
            return (input ?? string.Empty).Trim().Trim('"').Trim();
        }

        internal static string ValidateInput(string input)
        {
            var path = NormalizePath(input);
            if (path.Length == 0 || Directory.Exists(path))
            {
                return null;
            }

            if (File.Exists(path))
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                return extension == ".sln" || extension == ".csproj"
                    ? null
                    : "Indicare un file .sln o .csproj, oppure una cartella.";
            }

            return "Il percorso non esiste.";
        }

        private void RefreshCommands()
        {
            OnPropertyChanged(nameof(CanAnalyze));
            _browseFileCommand.RaiseCanExecuteChanged();
            _browseFolderCommand.RaiseCanExecuteChanged();
            _analyzeCommand.RaiseCanExecuteChanged();
            _cancelCommand.RaiseCanExecuteChanged();
            _saveReportCommand.RaiseCanExecuteChanged();
            _copyReportCommand.RaiseCanExecuteChanged();
            _openSolutionFolderCommand.RaiseCanExecuteChanged();
            _openFindingCommand.RaiseCanExecuteChanged();
            _revealFindingCommand.RaiseCanExecuteChanged();
            _copyFindingPathCommand.RaiseCanExecuteChanged();
            _openBacklogFileCommand.RaiseCanExecuteChanged();
            _clearFiltersCommand.RaiseCanExecuteChanged();
        }
    }
}
