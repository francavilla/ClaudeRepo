namespace DesktopAppTemplate.UI.WinForms.Views
{
    partial class TaskListView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Codice generato da Progettazione componenti

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.listCard = new DesktopAppTemplate.UI.WinForms.Components.CardPanel();
            this.grid = new System.Windows.Forms.DataGridView();
            this.toggleColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.titleColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.createdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.removeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.emptyLabel = new System.Windows.Forms.Label();
            this.listHeader = new System.Windows.Forms.Panel();
            this.headerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.listTitle = new System.Windows.Forms.Label();
            this.filterBox = new System.Windows.Forms.ComboBox();
            this.errorGap = new System.Windows.Forms.Panel();
            this.errorLabel = new System.Windows.Forms.Label();
            this.addGap = new System.Windows.Forms.Panel();
            this.addCard = new DesktopAppTemplate.UI.WinForms.Components.CardPanel();
            this.addLayout = new System.Windows.Forms.TableLayoutPanel();
            this.titleBox = new System.Windows.Forms.TextBox();
            this.addButton = new DesktopAppTemplate.UI.WinForms.Components.AccentButton();
            this.headerGap = new System.Windows.Forms.Panel();
            this.summaryLabel = new System.Windows.Forms.Label();
            this.pageTitleLabel = new System.Windows.Forms.Label();
            this.listCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.listHeader.SuspendLayout();
            this.headerLayout.SuspendLayout();
            this.addCard.SuspendLayout();
            this.addLayout.SuspendLayout();
            this.SuspendLayout();
            //
            // listCard
            //
            this.listCard.BackColor = System.Drawing.Color.White;
            this.listCard.Controls.Add(this.grid);
            this.listCard.Controls.Add(this.emptyLabel);
            this.listCard.Controls.Add(this.listHeader);
            this.listCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listCard.Location = new System.Drawing.Point(36, 200);
            this.listCard.Name = "listCard";
            this.listCard.Size = new System.Drawing.Size(728, 292);
            this.listCard.TabIndex = 7;
            //
            // grid
            //
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeColumns = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.grid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid.ColumnHeadersVisible = false;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.toggleColumn,
            this.titleColumn,
            this.createdColumn,
            this.removeColumn});
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.FromArgb(238, 242, 255);
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.grid.DefaultCellStyle = dataGridViewCellStyle1;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.EnableHeadersVisualStyles = false;
            this.grid.GridColor = System.Drawing.Color.FromArgb(229, 231, 235);
            this.grid.Location = new System.Drawing.Point(0, 56);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.RowTemplate.Height = 48;
            this.grid.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(728, 236);
            this.grid.TabIndex = 0;
            this.grid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.grid_CellClick);
            this.grid.CellMouseEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.grid_CellMouseEnter);
            this.grid.CellMouseLeave += new System.Windows.Forms.DataGridViewCellEventHandler(this.grid_CellMouseLeave);
            //
            // toggleColumn
            //
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe MDL2 Assets", 15F);
            this.toggleColumn.DefaultCellStyle = dataGridViewCellStyle2;
            this.toggleColumn.HeaderText = "Stato";
            this.toggleColumn.Name = "toggleColumn";
            this.toggleColumn.ReadOnly = true;
            this.toggleColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.toggleColumn.Width = 56;
            //
            // titleColumn
            //
            this.titleColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.titleColumn.HeaderText = "Titolo";
            this.titleColumn.Name = "titleColumn";
            this.titleColumn.ReadOnly = true;
            this.titleColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // createdColumn
            //
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.createdColumn.DefaultCellStyle = dataGridViewCellStyle3;
            this.createdColumn.HeaderText = "Creata il";
            this.createdColumn.Name = "createdColumn";
            this.createdColumn.ReadOnly = true;
            this.createdColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.createdColumn.Width = 150;
            //
            // removeColumn
            //
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe MDL2 Assets", 15F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.removeColumn.DefaultCellStyle = dataGridViewCellStyle4;
            this.removeColumn.HeaderText = "Elimina";
            this.removeColumn.Name = "removeColumn";
            this.removeColumn.ReadOnly = true;
            this.removeColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.removeColumn.Width = 56;
            //
            // emptyLabel
            //
            this.emptyLabel.BackColor = System.Drawing.Color.White;
            this.emptyLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.emptyLabel.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.emptyLabel.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.emptyLabel.Location = new System.Drawing.Point(0, 56);
            this.emptyLabel.Name = "emptyLabel";
            this.emptyLabel.Size = new System.Drawing.Size(728, 236);
            this.emptyLabel.TabIndex = 1;
            this.emptyLabel.Text = "Nessuna attività da mostrare.";
            this.emptyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // listHeader
            //
            this.listHeader.BackColor = System.Drawing.Color.White;
            this.listHeader.Controls.Add(this.headerLayout);
            this.listHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.listHeader.Location = new System.Drawing.Point(0, 0);
            this.listHeader.Name = "listHeader";
            this.listHeader.Padding = new System.Windows.Forms.Padding(20, 0, 20, 0);
            this.listHeader.Size = new System.Drawing.Size(728, 56);
            this.listHeader.TabIndex = 2;
            //
            // headerLayout
            //
            this.headerLayout.BackColor = System.Drawing.Color.Transparent;
            this.headerLayout.ColumnCount = 2;
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this.headerLayout.Controls.Add(this.listTitle, 0, 0);
            this.headerLayout.Controls.Add(this.filterBox, 1, 0);
            this.headerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerLayout.Location = new System.Drawing.Point(20, 0);
            this.headerLayout.Name = "headerLayout";
            this.headerLayout.RowCount = 1;
            this.headerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headerLayout.Size = new System.Drawing.Size(688, 56);
            this.headerLayout.TabIndex = 0;
            //
            // listTitle
            //
            this.listTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.listTitle.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.listTitle.Location = new System.Drawing.Point(3, 0);
            this.listTitle.Name = "listTitle";
            this.listTitle.Size = new System.Drawing.Size(512, 56);
            this.listTitle.TabIndex = 0;
            this.listTitle.Text = "Elenco";
            this.listTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // filterBox
            //
            this.filterBox.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.filterBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.filterBox.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.filterBox.FormattingEnabled = true;
            this.filterBox.Location = new System.Drawing.Point(521, 15);
            this.filterBox.Name = "filterBox";
            this.filterBox.Size = new System.Drawing.Size(164, 25);
            this.filterBox.TabIndex = 1;
            this.filterBox.SelectedIndexChanged += new System.EventHandler(this.filterBox_SelectedIndexChanged);
            //
            // errorGap
            //
            this.errorGap.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.errorGap.Dock = System.Windows.Forms.DockStyle.Top;
            this.errorGap.Location = new System.Drawing.Point(36, 188);
            this.errorGap.Name = "errorGap";
            this.errorGap.Size = new System.Drawing.Size(728, 12);
            this.errorGap.TabIndex = 6;
            //
            // errorLabel
            //
            this.errorLabel.BackColor = System.Drawing.Color.FromArgb(254, 226, 226);
            this.errorLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.errorLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.errorLabel.ForeColor = System.Drawing.Color.FromArgb(220, 38, 38);
            this.errorLabel.Location = new System.Drawing.Point(36, 144);
            this.errorLabel.Name = "errorLabel";
            this.errorLabel.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            this.errorLabel.Size = new System.Drawing.Size(728, 44);
            this.errorLabel.TabIndex = 5;
            this.errorLabel.Text = "Messaggio d'errore";
            this.errorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.errorLabel.Visible = false;
            //
            // addGap
            //
            this.addGap.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.addGap.Dock = System.Windows.Forms.DockStyle.Top;
            this.addGap.Location = new System.Drawing.Point(36, 132);
            this.addGap.Name = "addGap";
            this.addGap.Size = new System.Drawing.Size(728, 12);
            this.addGap.TabIndex = 4;
            //
            // addCard
            //
            this.addCard.BackColor = System.Drawing.Color.White;
            this.addCard.Controls.Add(this.addLayout);
            this.addCard.Dock = System.Windows.Forms.DockStyle.Top;
            this.addCard.Location = new System.Drawing.Point(36, 56);
            this.addCard.Name = "addCard";
            this.addCard.Padding = new System.Windows.Forms.Padding(16);
            this.addCard.Size = new System.Drawing.Size(728, 76);
            this.addCard.TabIndex = 3;
            //
            // addLayout
            //
            this.addLayout.BackColor = System.Drawing.Color.Transparent;
            this.addLayout.ColumnCount = 2;
            this.addLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.addLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.addLayout.Controls.Add(this.titleBox, 0, 0);
            this.addLayout.Controls.Add(this.addButton, 1, 0);
            this.addLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.addLayout.Location = new System.Drawing.Point(16, 16);
            this.addLayout.Name = "addLayout";
            this.addLayout.RowCount = 1;
            this.addLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.addLayout.Size = new System.Drawing.Size(696, 44);
            this.addLayout.TabIndex = 0;
            //
            // titleBox
            //
            this.titleBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.titleBox.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.titleBox.Location = new System.Drawing.Point(3, 8);
            this.titleBox.MaxLength = 200;
            this.titleBox.Name = "titleBox";
            this.titleBox.Size = new System.Drawing.Size(553, 29);
            this.titleBox.TabIndex = 0;
            this.titleBox.TextChanged += new System.EventHandler(this.titleBox_TextChanged);
            this.titleBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.titleBox_KeyDown);
            //
            // addButton
            //
            this.addButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.addButton.BackColor = System.Drawing.Color.FromArgb(79, 70, 229);
            this.addButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.addButton.FlatAppearance.BorderSize = 0;
            this.addButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(67, 56, 202);
            this.addButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(67, 56, 202);
            this.addButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.addButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.addButton.ForeColor = System.Drawing.Color.White;
            this.addButton.Location = new System.Drawing.Point(574, 3);
            this.addButton.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(119, 38);
            this.addButton.TabIndex = 1;
            this.addButton.Text = "Aggiungi";
            this.addButton.UseVisualStyleBackColor = false;
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            //
            // headerGap
            //
            this.headerGap.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.headerGap.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerGap.Location = new System.Drawing.Point(36, 48);
            this.headerGap.Name = "headerGap";
            this.headerGap.Size = new System.Drawing.Size(728, 8);
            this.headerGap.TabIndex = 2;
            //
            // summaryLabel
            //
            this.summaryLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.summaryLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.summaryLabel.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.summaryLabel.Location = new System.Drawing.Point(36, 32);
            this.summaryLabel.Name = "summaryLabel";
            this.summaryLabel.Size = new System.Drawing.Size(728, 28);
            this.summaryLabel.TabIndex = 1;
            this.summaryLabel.Text = "0 attività · 0 completate";
            //
            // pageTitleLabel
            //
            this.pageTitleLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.pageTitleLabel.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.pageTitleLabel.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.pageTitleLabel.Location = new System.Drawing.Point(36, 32);
            this.pageTitleLabel.Name = "pageTitleLabel";
            this.pageTitleLabel.Size = new System.Drawing.Size(728, 44);
            this.pageTitleLabel.TabIndex = 0;
            this.pageTitleLabel.Text = "Attività";
            //
            // TaskListView
            //
            this.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.Controls.Add(this.listCard);
            this.Controls.Add(this.errorGap);
            this.Controls.Add(this.errorLabel);
            this.Controls.Add(this.addGap);
            this.Controls.Add(this.addCard);
            this.Controls.Add(this.headerGap);
            this.Controls.Add(this.summaryLabel);
            this.Controls.Add(this.pageTitleLabel);
            this.Name = "TaskListView";
            this.Padding = new System.Windows.Forms.Padding(36, 32, 36, 28);
            this.Size = new System.Drawing.Size(800, 520);
            this.listCard.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.listHeader.ResumeLayout(false);
            this.headerLayout.ResumeLayout(false);
            this.addCard.ResumeLayout(false);
            this.addLayout.ResumeLayout(false);
            this.addLayout.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private DesktopAppTemplate.UI.WinForms.Components.CardPanel listCard;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.DataGridViewTextBoxColumn toggleColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn titleColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn createdColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn removeColumn;
        private System.Windows.Forms.Label emptyLabel;
        private System.Windows.Forms.Panel listHeader;
        private System.Windows.Forms.TableLayoutPanel headerLayout;
        private System.Windows.Forms.Label listTitle;
        private System.Windows.Forms.ComboBox filterBox;
        private System.Windows.Forms.Panel errorGap;
        private System.Windows.Forms.Label errorLabel;
        private System.Windows.Forms.Panel addGap;
        private DesktopAppTemplate.UI.WinForms.Components.CardPanel addCard;
        private System.Windows.Forms.TableLayoutPanel addLayout;
        private System.Windows.Forms.TextBox titleBox;
        private DesktopAppTemplate.UI.WinForms.Components.AccentButton addButton;
        private System.Windows.Forms.Panel headerGap;
        private System.Windows.Forms.Label summaryLabel;
        private System.Windows.Forms.Label pageTitleLabel;
    }
}
