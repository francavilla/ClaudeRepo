namespace DesktopAppTemplate.UI.WinForms
{
    partial class MainForm
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

        #region Codice generato da Progettazione Windows Form

        private void InitializeComponent()
        {
            this.contentPanel = new System.Windows.Forms.Panel();
            this.sidebar = new System.Windows.Forms.Panel();
            this.navPanel = new System.Windows.Forms.Panel();
            this.versionLabel = new System.Windows.Forms.Label();
            this.brandPanel = new System.Windows.Forms.Panel();
            this.brandNameLabel = new System.Windows.Forms.Label();
            this.brandSubtitleLabel = new System.Windows.Forms.Label();
            this.sidebar.SuspendLayout();
            this.brandPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // contentPanel
            //
            this.contentPanel.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentPanel.Location = new System.Drawing.Point(240, 0);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Size = new System.Drawing.Size(860, 720);
            this.contentPanel.TabIndex = 0;
            //
            // sidebar
            //
            this.sidebar.BackColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.sidebar.Controls.Add(this.navPanel);
            this.sidebar.Controls.Add(this.versionLabel);
            this.sidebar.Controls.Add(this.brandPanel);
            this.sidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.sidebar.Location = new System.Drawing.Point(0, 0);
            this.sidebar.Name = "sidebar";
            this.sidebar.Size = new System.Drawing.Size(240, 720);
            this.sidebar.TabIndex = 1;
            //
            // navPanel
            //
            this.navPanel.BackColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.navPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.navPanel.Location = new System.Drawing.Point(0, 84);
            this.navPanel.Name = "navPanel";
            this.navPanel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.navPanel.Size = new System.Drawing.Size(240, 596);
            this.navPanel.TabIndex = 2;
            //
            // versionLabel
            //
            this.versionLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.versionLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.versionLabel.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.versionLabel.Location = new System.Drawing.Point(0, 680);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Padding = new System.Windows.Forms.Padding(24, 0, 0, 0);
            this.versionLabel.Size = new System.Drawing.Size(240, 40);
            this.versionLabel.TabIndex = 1;
            this.versionLabel.Text = "v1.0.0";
            this.versionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // brandPanel
            //
            this.brandPanel.Controls.Add(this.brandNameLabel);
            this.brandPanel.Controls.Add(this.brandSubtitleLabel);
            this.brandPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.brandPanel.Location = new System.Drawing.Point(0, 0);
            this.brandPanel.Name = "brandPanel";
            this.brandPanel.Size = new System.Drawing.Size(240, 84);
            this.brandPanel.TabIndex = 0;
            //
            // brandNameLabel
            //
            this.brandNameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.brandNameLabel.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.brandNameLabel.ForeColor = System.Drawing.Color.White;
            this.brandNameLabel.Location = new System.Drawing.Point(0, 0);
            this.brandNameLabel.Name = "brandNameLabel";
            this.brandNameLabel.Padding = new System.Windows.Forms.Padding(24, 0, 0, 2);
            this.brandNameLabel.Size = new System.Drawing.Size(240, 60);
            this.brandNameLabel.TabIndex = 0;
            this.brandNameLabel.Text = "DesktopAppTemplate";
            this.brandNameLabel.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            //
            // brandSubtitleLabel
            //
            this.brandSubtitleLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.brandSubtitleLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.brandSubtitleLabel.ForeColor = System.Drawing.Color.FromArgb(156, 163, 175);
            this.brandSubtitleLabel.Location = new System.Drawing.Point(0, 60);
            this.brandSubtitleLabel.Name = "brandSubtitleLabel";
            this.brandSubtitleLabel.Padding = new System.Windows.Forms.Padding(24, 0, 0, 0);
            this.brandSubtitleLabel.Size = new System.Drawing.Size(240, 24);
            this.brandSubtitleLabel.TabIndex = 1;
            this.brandSubtitleLabel.Text = "Applicazione desktop";
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.ClientSize = new System.Drawing.Size(1100, 720);
            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.sidebar);
            this.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.MinimumSize = new System.Drawing.Size(900, 620);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DesktopAppTemplate";
            this.sidebar.ResumeLayout(false);
            this.brandPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel contentPanel;
        private System.Windows.Forms.Panel sidebar;
        private System.Windows.Forms.Panel navPanel;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.Panel brandPanel;
        private System.Windows.Forms.Label brandNameLabel;
        private System.Windows.Forms.Label brandSubtitleLabel;
    }
}
