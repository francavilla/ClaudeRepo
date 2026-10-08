using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Messaging;
using DesktopAppTemplate.Core.Mvvm;
using DesktopAppTemplate.Features.Tasks.AddTask;
using DesktopAppTemplate.Features.Tasks.ListTasks;
using DesktopAppTemplate.Features.Tasks.RemoveTask;
using DesktopAppTemplate.Features.Tasks.ToggleTask;

namespace DesktopAppTemplate.Features.Tasks
{
    /// <summary>Pagina "Attività": identica per WPF e Windows Forms, che si limitano a mostrarla.</summary>
    public sealed class TaskListViewModel : PageViewModel
    {
        private readonly IMediator _mediator;
        private readonly IDialogService _dialogs;
        private readonly TaskSettings _settings;

        private string _newTitle = string.Empty;
        private FilterOption _selectedFilter;
        private string _summary = string.Empty;
        private string _errorMessage;
        private bool _isEmpty = true;

        public TaskListViewModel(IMediator mediator, IDialogService dialogs, TaskSettings settings)
            : base("Attività", "\uE73A", 10)
        {
            _mediator = mediator;
            _dialogs = dialogs;
            _settings = settings;

            FilterOptions = new List<FilterOption>
            {
                new FilterOption("Tutte", TaskFilter.All),
                new FilterOption("Da fare", TaskFilter.Active),
                new FilterOption("Completate", TaskFilter.Completed)
            };
            _selectedFilter = FilterOptions[0];

            AddCommand = new AsyncRelayCommand(AddAsync, () => CanEdit && !string.IsNullOrWhiteSpace(NewTitle));
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            ToggleCommand = new AsyncRelayCommand<TaskItemViewModel>(ToggleAsync, item => CanEdit && item != null);
            RemoveCommand = new AsyncRelayCommand<TaskItemViewModel>(RemoveAsync, item => CanEdit && item != null);

            AddCommand.ErrorHandler = HandleError;
            RefreshCommand.ErrorHandler = HandleError;
            ToggleCommand.ErrorHandler = HandleError;
            RemoveCommand.ErrorHandler = HandleError;
        }

        public ObservableCollection<TaskItemViewModel> Items { get; } = new ObservableCollection<TaskItemViewModel>();

        public IReadOnlyList<FilterOption> FilterOptions { get; }

        public AsyncRelayCommand AddCommand { get; }
        public AsyncRelayCommand RefreshCommand { get; }
        public AsyncRelayCommand<TaskItemViewModel> ToggleCommand { get; }
        public AsyncRelayCommand<TaskItemViewModel> RemoveCommand { get; }

        public string NewTitle
        {
            get { return _newTitle; }
            set
            {
                if (SetProperty(ref _newTitle, value ?? string.Empty))
                    AddCommand.RaiseCanExecuteChanged();
            }
        }

        public FilterOption SelectedFilter
        {
            get { return _selectedFilter; }
            set
            {
                if (value != null && SetProperty(ref _selectedFilter, value))
                    RefreshCommand.Execute(null);
            }
        }

        /// <summary>Riepilogo del tipo "5 attività · 2 completate".</summary>
        public string Summary
        {
            get { return _summary; }
            private set { SetProperty(ref _summary, value); }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                    OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>True se l'elenco (filtrato) non contiene attività.</summary>
        public bool IsEmpty
        {
            get { return _isEmpty; }
            private set { SetProperty(ref _isEmpty, value); }
        }

        /// <summary>False in modalità sola lettura (opzione <c>--read-only</c>).</summary>
        public bool CanEdit => !_settings.ReadOnly;

        public string EmptyMessage => "Nessuna attività da mostrare.";

        public override Task OnNavigatedToAsync() => RefreshCommand.ExecuteAsync();

        private Task RefreshAsync()
        {
            ErrorMessage = null;
            return LoadAsync();
        }

        private async Task LoadAsync()
        {
            var result = await _mediator.Send(new ListTasksQuery(SelectedFilter.Value));

            Items.Clear();
            foreach (var item in result.Items)
                Items.Add(new TaskItemViewModel(item));

            Summary = $"{result.TotalCount} attività · {result.CompletedCount} completate"
                      + (CanEdit ? string.Empty : " · sola lettura");
            IsEmpty = Items.Count == 0;
        }

        private async Task AddAsync()
        {
            ErrorMessage = null;
            await _mediator.Send(new AddTaskCommand(NewTitle));
            NewTitle = string.Empty;
            await LoadAsync();
        }

        private async Task ToggleAsync(TaskItemViewModel item)
        {
            ErrorMessage = null;
            await _mediator.Send(new ToggleTaskCommand(item.Id));
            await LoadAsync();
        }

        private async Task RemoveAsync(TaskItemViewModel item)
        {
            ErrorMessage = null;
            if (!_dialogs.Confirm("Elimina attività", $"Eliminare l'attività \"{item.Title}\"?"))
                return;

            await _mediator.Send(new RemoveTaskCommand(item.Id));
            await LoadAsync();
        }

        private void HandleError(Exception exception)
        {
            var validation = exception as ValidationException;
            ErrorMessage = validation != null
                ? string.Join(Environment.NewLine, validation.Errors)
                : exception.Message;
        }
    }
}
