namespace DesktopAppTemplate.UI.WinForms.Views
{
    partial class AboutView
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
            this.card = new DesktopAppTemplate.UI.WinForms.Components.CardPanel();
            this.layout = new System.Windows.Forms.TableLayoutPanel();
            this.nameLabel = new System.Windows.Forms.Label();
            this.descriptionLabel = new System.Windows.Forms.Label();
            this.uiCaptionLabel = new System.Windows.Forms.Label();
            this.uiValueLabel = new System.Windows.Forms.Label();
            this.runtimeCaptionLabel = new System.Windows.Forms.Label();
            this.runtimeValueLabel = new System.Windows.Forms.Label();
            this.userCaptionLabel = new System.Windows.Forms.Label();
            this.userValueLabel = new System.Windows.Forms.Label();
            this.machineCaptionLabel = new System.Windows.Forms.Label();
            this.machineValueLabel = new System.Windows.Forms.Label();
            this.cardGap = new System.Windows.Forms.Panel();
            this.configCard = new DesktopAppTemplate.UI.WinForms.Components.CardPanel();
            this.configList = new System.Windows.Forms.ListView();
            this.keyColumn = new System.Windows.Forms.ColumnHeader();
            this.valueColumn = new System.Windows.Forms.ColumnHeader();
            this.sourceColumn = new System.Windows.Forms.ColumnHeader();
            this.configTitleLabel = new System.Windows.Forms.Label();
            this.gap = new System.Windows.Forms.Panel();
            this.pageTitleLabel = new System.Windows.Forms.Label();
            this.card.SuspendLayout();
            this.layout.SuspendLayout();
            this.configCard.SuspendLayout();
            this.SuspendLayout();
            //
            // card
            //
            this.card.BackColor = System.Drawing.Color.White;
            this.card.Controls.Add(this.layout);
            this.card.Dock = System.Windows.Forms.DockStyle.Top;
            this.card.Location = new System.Drawing.Point(36, 68);
            this.card.Name = "card";
            this.card.Padding = new System.Windows.Forms.Padding(28);
            this.card.Size = new System.Drawing.Size(728, 290);
            this.card.TabIndex = 2;
            //
            // layout
            //
            this.layout.BackColor = System.Drawing.Color.Transparent;
            this.layout.ColumnCount = 2;
            this.layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this.layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layout.Controls.Add(this.nameLabel, 0, 0);
            this.layout.Controls.Add(this.descriptionLabel, 0, 1);
            this.layout.Controls.Add(this.uiCaptionLabel, 0, 2);
            this.layout.Controls.Add(this.uiValueLabel, 1, 2);
            this.layout.Controls.Add(this.runtimeCaptionLabel, 0, 3);
            this.layout.Controls.Add(this.runtimeValueLabel, 1, 3);
            this.layout.Controls.Add(this.userCaptionLabel, 0, 4);
            this.layout.Controls.Add(this.userValueLabel, 1, 4);
            this.layout.Controls.Add(this.machineCaptionLabel, 0, 5);
            this.layout.Controls.Add(this.machineValueLabel, 1, 5);
            this.layout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layout.Location = new System.Drawing.Point(28, 28);
            this.layout.Name = "layout";
            this.layout.RowCount = 6;
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.layout.Size = new System.Drawing.Size(672, 234);
            this.layout.TabIndex = 0;
            //
            // nameLabel
            //
            this.layout.SetColumnSpan(this.nameLabel, 2);
            this.nameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nameLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.nameLabel.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.nameLabel.Location = new System.Drawing.Point(3, 0);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(666, 44);
            this.nameLabel.TabIndex = 0;
            this.nameLabel.Text = "DesktopAppTemplate   v1.0.0";
            this.nameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // descriptionLabel
            //
            this.layout.SetColumnSpan(this.descriptionLabel, 2);
            this.descriptionLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.descriptionLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.descriptionLabel.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.descriptionLabel.Location = new System.Drawing.Point(3, 44);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(666, 56);
            this.descriptionLabel.TabIndex = 1;
            this.descriptionLabel.Text = "Descrizione";
            //
            // uiCaptionLabel
            //
            this.uiCaptionLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiCaptionLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.uiCaptionLabel.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.uiCaptionLabel.Location = new System.Drawing.Point(3, 100);
            this.uiCaptionLabel.Name = "uiCaptionLabel";
            this.uiCaptionLabel.Size = new System.Drawing.Size(164, 30);
            this.uiCaptionLabel.TabIndex = 2;
            this.uiCaptionLabel.Text = "Interfaccia attiva";
            this.uiCaptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // uiValueLabel
            //
            this.uiValueLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiValueLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.uiValueLabel.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.uiValueLabel.Location = new System.Drawing.Point(173, 100);
            this.uiValueLabel.Name = "uiValueLabel";
            this.uiValueLabel.Size = new System.Drawing.Size(496, 30);
            this.uiValueLabel.TabIndex = 3;
            this.uiValueLabel.Text = "Windows Forms";
            this.uiValueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // runtimeCaptionLabel
            //
            this.runtimeCaptionLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.runtimeCaptionLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.runtimeCaptionLabel.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.runtimeCaptionLabel.Location = new System.Drawing.Point(3, 130);
            this.runtimeCaptionLabel.Name = "runtimeCaptionLabel";
            this.runtimeCaptionLabel.Size = new System.Drawing.Size(164, 30);
            this.runtimeCaptionLabel.TabIndex = 4;
            this.runtimeCaptionLabel.Text = "Runtime";
            this.runtimeCaptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // runtimeValueLabel
            //
            this.runtimeValueLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.runtimeValueLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.runtimeValueLabel.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.runtimeValueLabel.Location = new System.Drawing.Point(173, 130);
            this.runtimeValueLabel.Name = "runtimeValueLabel";
            this.runtimeValueLabel.Size = new System.Drawing.Size(496, 30);
            this.runtimeValueLabel.TabIndex = 5;
            this.runtimeValueLabel.Text = ".NET Framework";
            this.runtimeValueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // userCaptionLabel
            //
            this.userCaptionLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userCaptionLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.userCaptionLabel.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.userCaptionLabel.Location = new System.Drawing.Point(3, 160);
            this.userCaptionLabel.Name = "userCaptionLabel";
            this.userCaptionLabel.Size = new System.Drawing.Size(164, 30);
            this.userCaptionLabel.TabIndex = 6;
            this.userCaptionLabel.Text = "Utente";
            this.userCaptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // userValueLabel
            //
            this.userValueLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userValueLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.userValueLabel.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.userValueLabel.Location = new System.Drawing.Point(173, 160);
            this.userValueLabel.Name = "userValueLabel";
            this.userValueLabel.Size = new System.Drawing.Size(496, 30);
            this.userValueLabel.TabIndex = 7;
            this.userValueLabel.Text = "Utente";
            this.userValueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // machineCaptionLabel
            //
            this.machineCaptionLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.machineCaptionLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.machineCaptionLabel.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.machineCaptionLabel.Location = new System.Drawing.Point(3, 190);
            this.machineCaptionLabel.Name = "machineCaptionLabel";
            this.machineCaptionLabel.Size = new System.Drawing.Size(164, 30);
            this.machineCaptionLabel.TabIndex = 8;
            this.machineCaptionLabel.Text = "Computer";
            this.machineCaptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // machineValueLabel
            //
            this.machineValueLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.machineValueLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.machineValueLabel.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.machineValueLabel.Location = new System.Drawing.Point(173, 190);
            this.machineValueLabel.Name = "machineValueLabel";
            this.machineValueLabel.Size = new System.Drawing.Size(496, 30);
            this.machineValueLabel.TabIndex = 9;
            this.machineValueLabel.Text = "Computer";
            this.machineValueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cardGap
            //
            this.cardGap.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.cardGap.Dock = System.Windows.Forms.DockStyle.Top;
            this.cardGap.Location = new System.Drawing.Point(36, 358);
            this.cardGap.Name = "cardGap";
            this.cardGap.Size = new System.Drawing.Size(728, 12);
            this.cardGap.TabIndex = 3;
            //
            // configCard
            //
            this.configCard.BackColor = System.Drawing.Color.White;
            this.configCard.Controls.Add(this.configList);
            this.configCard.Controls.Add(this.configTitleLabel);
            this.configCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configCard.Location = new System.Drawing.Point(36, 370);
            this.configCard.Name = "configCard";
            this.configCard.Padding = new System.Windows.Forms.Padding(28, 16, 28, 16);
            this.configCard.Size = new System.Drawing.Size(728, 122);
            this.configCard.TabIndex = 4;
            //
            // configList
            //
            this.configList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.configList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.keyColumn,
            this.valueColumn,
            this.sourceColumn});
            this.configList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configList.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.configList.FullRowSelect = true;
            this.configList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.configList.Location = new System.Drawing.Point(28, 52);
            this.configList.MultiSelect = false;
            this.configList.Name = "configList";
            this.configList.Size = new System.Drawing.Size(672, 54);
            this.configList.TabIndex = 1;
            this.configList.UseCompatibleStateImageBehavior = false;
            this.configList.View = System.Windows.Forms.View.Details;
            //
            // keyColumn
            //
            this.keyColumn.Text = "Parametro";
            this.keyColumn.Width = 170;
            //
            // valueColumn
            //
            this.valueColumn.Text = "Valore";
            this.valueColumn.Width = 320;
            //
            // sourceColumn
            //
            this.sourceColumn.Text = "Origine";
            this.sourceColumn.Width = 160;
            //
            // configTitleLabel
            //
            this.configTitleLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.configTitleLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.configTitleLabel.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.configTitleLabel.Location = new System.Drawing.Point(28, 16);
            this.configTitleLabel.Name = "configTitleLabel";
            this.configTitleLabel.Size = new System.Drawing.Size(672, 36);
            this.configTitleLabel.TabIndex = 0;
            this.configTitleLabel.Text = "Configurazione attiva";
            this.configTitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // gap
            //
            this.gap.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.gap.Dock = System.Windows.Forms.DockStyle.Top;
            this.gap.Location = new System.Drawing.Point(36, 56);
            this.gap.Name = "gap";
            this.gap.Size = new System.Drawing.Size(728, 12);
            this.gap.TabIndex = 1;
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
            this.pageTitleLabel.Text = "Informazioni";
            //
            // AboutView
            //
            this.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.Controls.Add(this.configCard);
            this.Controls.Add(this.cardGap);
            this.Controls.Add(this.card);
            this.Controls.Add(this.gap);
            this.Controls.Add(this.pageTitleLabel);
            this.Name = "AboutView";
            this.Padding = new System.Windows.Forms.Padding(36, 32, 36, 28);
            this.Size = new System.Drawing.Size(800, 520);
            this.card.ResumeLayout(false);
            this.layout.ResumeLayout(false);
            this.configCard.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private DesktopAppTemplate.UI.WinForms.Components.CardPanel card;
        private System.Windows.Forms.TableLayoutPanel layout;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.Label descriptionLabel;
        private System.Windows.Forms.Label uiCaptionLabel;
        private System.Windows.Forms.Label uiValueLabel;
        private System.Windows.Forms.Label runtimeCaptionLabel;
        private System.Windows.Forms.Label runtimeValueLabel;
        private System.Windows.Forms.Label userCaptionLabel;
        private System.Windows.Forms.Label userValueLabel;
        private System.Windows.Forms.Label machineCaptionLabel;
        private System.Windows.Forms.Label machineValueLabel;
        private System.Windows.Forms.Panel cardGap;
        private DesktopAppTemplate.UI.WinForms.Components.CardPanel configCard;
        private System.Windows.Forms.ListView configList;
        private System.Windows.Forms.ColumnHeader keyColumn;
        private System.Windows.Forms.ColumnHeader valueColumn;
        private System.Windows.Forms.ColumnHeader sourceColumn;
        private System.Windows.Forms.Label configTitleLabel;
        private System.Windows.Forms.Panel gap;
        private System.Windows.Forms.Label pageTitleLabel;
    }
}
