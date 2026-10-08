using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DesktopAppTemplate.Features.Tasks;
using DesktopAppTemplate.UI.WinForms.Components;

namespace DesktopAppTemplate.UI.WinForms.Views
{
    /// <summary>
    /// Pagina "Attività" in Windows Forms. Contiene solo codice di presentazione:
    /// legge lo stato dal <see cref="TaskListViewModel"/> e ne invoca i comandi.
    /// </summary>
    internal sealed class TaskListView : UserControl
    {
        private const int ToggleColumn = 0;
        private const int RemoveColumn = 3;
        private const string ToggleOff = "";
        private const string ToggleOn = "";
        private const string RemoveGlyph = "";

        private readonly TaskListViewModel _viewModel;

        private readonly Label _summaryLabel;
        private readonly TextBox _titleBox;
        private readonly AccentButton _addButton;
        private readonly Label _errorLabel;
        private readonly ComboBox _filterBox;
        private readonly DataGridView _grid;
        private readonly Label _emptyLabel;
        private bool _rebuildPending;

        public TaskListView(TaskListViewModel viewModel)
        {
            _viewModel = viewModel;

            BackColor = Palette.Background;
            Padding = new Padding(36, 32, 36, 28);

            // --- Elenco (card che occupa lo spazio rimanente) ---
            var listCard = new CardPanel { Dock = DockStyle.Fill };

            _grid = CreateGrid();
            _emptyLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = viewModel.EmptyMessage,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Palette.Muted,
                Font = Palette.Ui(10.5f),
                BackColor = Palette.Surface
            };

            var listHeader = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Palette.Surface, Padding = new Padding(20, 0, 20, 0) };
            var listTitle = new Label
            {
                Text = "Elenco",
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 120,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = Palette.Ui(11f, FontStyle.Bold),
                ForeColor = Palette.Text
            };
            _filterBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 160,
                Font = Palette.Ui(10f),
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.None
            };
            foreach (var option in viewModel.FilterOptions)
                _filterBox.Items.Add(option);
            _filterBox.DisplayMember = nameof(FilterOption.Label);
            var filterHost = new Panel { Dock = DockStyle.Right, Width = 170, BackColor = Palette.Surface };
            filterHost.Controls.Add(_filterBox);
            filterHost.Resize += (s, e) => _filterBox.Location = new Point(filterHost.Width - _filterBox.Width, (filterHost.Height - _filterBox.Height) / 2);
            listHeader.Controls.Add(listTitle);
            listHeader.Controls.Add(filterHost);

            // Il controllo "Fill" si aggiunge per primo, poi quelli agganciati ai bordi.
            listCard.Controls.Add(_grid);
            listCard.Controls.Add(_emptyLabel);
            listCard.Controls.Add(listHeader);

            // --- Messaggio d'errore ---
            _errorLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 44,
                BackColor = Palette.DangerSoft,
                ForeColor = Palette.Danger,
                Font = Palette.Ui(10f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 14, 0),
                Visible = false
            };
            var errorGap = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = Palette.Background };

            // --- Nuova attività ---
            var addCard = new CardPanel { Dock = DockStyle.Top, Height = 76, Padding = new Padding(16) };
            _titleBox = new TextBox
            {
                Font = Palette.Ui(12f),
                MaxLength = 200,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            _addButton = new AccentButton { Text = "Aggiungi", Size = new Size(120, 38), Anchor = AnchorStyles.Right };
            var addLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            addLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            addLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            addLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            addLayout.Controls.Add(_titleBox, 0, 0);
            addLayout.Controls.Add(_addButton, 1, 0);
            _addButton.Margin = new Padding(12, 0, 0, 0);
            addCard.Controls.Add(addLayout);
            var addGap = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = Palette.Background };

            // --- Intestazione ---
            var title = new Label
            {
                Text = viewModel.Title,
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 44,
                Font = Palette.Ui(20f, FontStyle.Bold),
                ForeColor = Palette.Text
            };
            _summaryLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 30,
                Font = Palette.Ui(10f),
                ForeColor = Palette.Muted
            };
            var headerGap = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Palette.Background };

            // Z-order: il controllo "Fill" per primo, poi i bordi dal basso verso l'alto.
            Controls.Add(listCard);
            Controls.Add(errorGap);
            Controls.Add(_errorLabel);
            Controls.Add(addGap);
            Controls.Add(addCard);
            Controls.Add(headerGap);
            Controls.Add(_summaryLabel);
            Controls.Add(title);

            WireUp();
            Sync();
            RebuildGrid();
        }

        private DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                RowHeadersVisible = false,
                ColumnHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                BackgroundColor = Palette.Surface,
                GridColor = Palette.Line,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ScrollBars = ScrollBars.Vertical
            };
            grid.RowTemplate.Height = 48;
            grid.DefaultCellStyle.Font = Palette.Ui(10.5f);
            grid.DefaultCellStyle.ForeColor = Palette.Text;
            grid.DefaultCellStyle.BackColor = Palette.Surface;
            grid.DefaultCellStyle.SelectionBackColor = Palette.AccentSoft;
            grid.DefaultCellStyle.SelectionForeColor = Palette.Text;
            grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);

            var iconStyle = new DataGridViewCellStyle(grid.DefaultCellStyle)
            {
                Font = Palette.Icons(15f),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            var dateStyle = new DataGridViewCellStyle(grid.DefaultCellStyle)
            {
                Font = Palette.Ui(9f),
                ForeColor = Palette.Muted,
                Alignment = DataGridViewContentAlignment.MiddleRight
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { Width = 56, DefaultCellStyle = iconStyle, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Width = 150, DefaultCellStyle = dateStyle, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Width = 56, DefaultCellStyle = iconStyle, SortMode = DataGridViewColumnSortMode.NotSortable });
            return grid;
        }

        private void WireUp()
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.Items.CollectionChanged += OnItemsChanged;

            _titleBox.TextChanged += (s, e) => _viewModel.NewTitle = _titleBox.Text;
            _titleBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                if (_viewModel.AddCommand.CanExecute(null))
                    _viewModel.AddCommand.Execute(null);
            };
            NativeMethods.SetCueBanner(_titleBox, "Cosa devi fare?");

            _addButton.Click += (s, e) => _viewModel.AddCommand.Execute(null);
            _viewModel.AddCommand.CanExecuteChanged += (s, e) => _addButton.Enabled = _viewModel.AddCommand.CanExecute(null);

            _filterBox.SelectedIndexChanged += (s, e) =>
            {
                var option = _filterBox.SelectedItem as FilterOption;
                if (option != null)
                    _viewModel.SelectedFilter = option;
            };

            _grid.CellClick += OnGridCellClick;
            _grid.CellMouseEnter += (s, e) =>
                _grid.Cursor = e.RowIndex >= 0 && (e.ColumnIndex == ToggleColumn || e.ColumnIndex == RemoveColumn) ? Cursors.Hand : Cursors.Default;
            _grid.CellMouseLeave += (s, e) => _grid.Cursor = Cursors.Default;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Sync();
        }

        /// <summary>Allinea i controlli semplici allo stato del view model.</summary>
        private void Sync()
        {
            _summaryLabel.Text = _viewModel.Summary;

            _errorLabel.Text = _viewModel.ErrorMessage ?? string.Empty;
            _errorLabel.Visible = _viewModel.HasError;

            if (_titleBox.Text != _viewModel.NewTitle)
                _titleBox.Text = _viewModel.NewTitle;

            _addButton.Enabled = _viewModel.AddCommand.CanExecute(null);

            if (!ReferenceEquals(_filterBox.SelectedItem, _viewModel.SelectedFilter))
                _filterBox.SelectedItem = _viewModel.SelectedFilter;

            _grid.Visible = !_viewModel.IsEmpty;
            _emptyLabel.Visible = _viewModel.IsEmpty;
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

            _grid.SuspendLayout();
            _grid.Rows.Clear();
            foreach (var item in _viewModel.Items)
            {
                var index = _grid.Rows.Add(item.IsCompleted ? ToggleOn : ToggleOff, item.Title, item.CreatedText, RemoveGlyph);
                var row = _grid.Rows[index];
                row.Tag = item;

                row.Cells[ToggleColumn].Style.ForeColor = item.IsCompleted ? Palette.Accent : Palette.Muted;
                row.Cells[RemoveColumn].Style.ForeColor = Palette.Muted;
                if (item.IsCompleted)
                {
                    row.Cells[1].Style.ForeColor = Palette.Muted;
                    row.Cells[1].Style.Font = Palette.Ui(10.5f, FontStyle.Strikeout);
                }
            }

            _grid.ClearSelection();
            _grid.ResumeLayout();
            Sync();
        }

        private void OnGridCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var item = _grid.Rows[e.RowIndex].Tag as TaskItemViewModel;
            if (item == null) return;

            // I comandi possono completare in modo sincrono e ricostruire la griglia: li si esegue
            // dopo il termine dell'evento di click.
            if (e.ColumnIndex == ToggleColumn)
                BeginInvoke((Action)(() => _viewModel.ToggleCommand.Execute(item)));
            else if (e.ColumnIndex == RemoveColumn)
                BeginInvoke((Action)(() => _viewModel.RemoveCommand.Execute(item)));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.Items.CollectionChanged -= OnItemsChanged;
            }

            base.Dispose(disposing);
        }
    }
}
