using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DesktopAppTemplate.Features.Tasks;

namespace DesktopAppTemplate.UI.WinForms.Views
{
    /// <summary>
    /// Pagina "Attività" in Windows Forms. L'aspetto è nel designer (TaskListView.Designer.cs);
    /// qui c'è solo il collegamento al <see cref="TaskListViewModel"/>: legge lo stato e ne invoca i comandi.
    /// </summary>
    public partial class TaskListView : UserControl
    {
        private const string ToggleOff = "";
        private const string ToggleOn = "";
        private const string RemoveGlyph = "";

        private TaskListViewModel _viewModel;
        private bool _rebuildPending;

        /// <summary>Costruttore per il designer; a runtime chiamare poi <see cref="Bind"/>.</summary>
        public TaskListView()
        {
            InitializeComponent();
        }

        public TaskListView(TaskListViewModel viewModel)
            : this()
        {
            Bind(viewModel);
        }

        /// <summary>Collega la view al view model.</summary>
        public void Bind(TaskListViewModel viewModel)
        {
            if (_viewModel != null)
                throw new InvalidOperationException("La view è già collegata a un view model.");

            _viewModel = viewModel;

            pageTitleLabel.Text = viewModel.Title;
            emptyLabel.Text = viewModel.EmptyMessage;

            foreach (var option in viewModel.FilterOptions)
                filterBox.Items.Add(option);
            filterBox.DisplayMember = nameof(FilterOption.Label);

            NativeMethods.SetCueBanner(titleBox, "Cosa devi fare?");

            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.Items.CollectionChanged += OnItemsChanged;
            viewModel.AddCommand.CanExecuteChanged += OnAddCanExecuteChanged;
            Disposed += (s, e) => Unbind();

            Sync();
            RebuildGrid();
        }

        private void Unbind()
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.Items.CollectionChanged -= OnItemsChanged;
            _viewModel.AddCommand.CanExecuteChanged -= OnAddCanExecuteChanged;
        }

        // --- Eventi dei controlli (collegati dal designer) ---

        private void titleBox_TextChanged(object sender, EventArgs e)
        {
            if (_viewModel != null)
                _viewModel.NewTitle = titleBox.Text;
        }

        private void titleBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (_viewModel == null || e.KeyCode != Keys.Enter) return;

            e.SuppressKeyPress = true;
            if (_viewModel.AddCommand.CanExecute(null))
                _viewModel.AddCommand.Execute(null);
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            _viewModel?.AddCommand.Execute(null);
        }

        private void filterBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var option = filterBox.SelectedItem as FilterOption;
            if (_viewModel != null && option != null)
                _viewModel.SelectedFilter = option;
        }

        private void grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_viewModel == null || e.RowIndex < 0) return;

            var item = grid.Rows[e.RowIndex].Tag as TaskItemViewModel;
            if (item == null) return;

            // I comandi possono completare in modo sincrono e ricostruire la griglia: li si esegue
            // dopo il termine dell'evento di click.
            if (e.ColumnIndex == toggleColumn.Index)
                BeginInvoke((Action)(() => _viewModel.ToggleCommand.Execute(item)));
            else if (e.ColumnIndex == removeColumn.Index)
                BeginInvoke((Action)(() => _viewModel.RemoveCommand.Execute(item)));
        }

        private void grid_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            var clickable = _viewModel != null && _viewModel.CanEdit && e.RowIndex >= 0
                            && (e.ColumnIndex == toggleColumn.Index || e.ColumnIndex == removeColumn.Index);
            grid.Cursor = clickable ? Cursors.Hand : Cursors.Default;
        }

        private void grid_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            grid.Cursor = Cursors.Default;
        }

        // --- Allineamento allo stato del view model ---

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Sync();
        }

        private void OnAddCanExecuteChanged(object sender, EventArgs e)
        {
            addButton.Enabled = _viewModel.AddCommand.CanExecute(null);
        }

        /// <summary>Allinea i controlli semplici allo stato del view model.</summary>
        private void Sync()
        {
            summaryLabel.Text = _viewModel.Summary;

            errorLabel.Text = _viewModel.ErrorMessage ?? string.Empty;
            errorLabel.Visible = _viewModel.HasError;

            if (titleBox.Text != _viewModel.NewTitle)
                titleBox.Text = _viewModel.NewTitle;

            addButton.Enabled = _viewModel.AddCommand.CanExecute(null);
            titleBox.Enabled = _viewModel.CanEdit;

            if (!ReferenceEquals(filterBox.SelectedItem, _viewModel.SelectedFilter))
                filterBox.SelectedItem = _viewModel.SelectedFilter;

            grid.Visible = !_viewModel.IsEmpty;
            emptyLabel.Visible = _viewModel.IsEmpty;
        }

        private void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Il view model svuota e riempie l'elenco: si ricostruisce la griglia una sola volta.
            if (_rebuildPending) return;

            if (!IsHandleCreated)
            {
                RebuildGrid();
                return;
            }

            _rebuildPending = true;
            BeginInvoke((Action)RebuildGrid);
        }

        private void RebuildGrid()
        {
            _rebuildPending = false;

            grid.SuspendLayout();
            grid.Rows.Clear();
            foreach (var item in _viewModel.Items)
            {
                var index = grid.Rows.Add(item.IsCompleted ? ToggleOn : ToggleOff, item.Title, item.CreatedText, RemoveGlyph);
                var row = grid.Rows[index];
                row.Tag = item;

                row.Cells[toggleColumn.Index].Style.ForeColor = item.IsCompleted ? Palette.Accent : Palette.Muted;
                if (item.IsCompleted)
                {
                    row.Cells[titleColumn.Index].Style.ForeColor = Palette.Muted;
                    row.Cells[titleColumn.Index].Style.Font = new Font(grid.DefaultCellStyle.Font, FontStyle.Strikeout);
                }
            }

            grid.ClearSelection();
            grid.ResumeLayout();
            Sync();
        }
    }
}
