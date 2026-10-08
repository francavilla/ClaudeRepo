using System.Drawing;
using System.Windows.Forms;
using DesktopAppTemplate.Features.About;
using DesktopAppTemplate.UI.WinForms.Components;

namespace DesktopAppTemplate.UI.WinForms.Views
{
    /// <summary>Pagina "Informazioni" in Windows Forms.</summary>
    internal sealed class AboutView : UserControl
    {
        public AboutView(AboutViewModel viewModel)
        {
            BackColor = Palette.Background;
            Padding = new Padding(36, 32, 36, 28);

            var card = new CardPanel { Dock = DockStyle.Top, Height = 230, Padding = new Padding(28) };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));

            var name = new Label
            {
                Text = viewModel.AppName + "   " + viewModel.Version,
                Font = Palette.Ui(16f, FontStyle.Bold),
                ForeColor = Palette.Text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(name, 0, 0);
            layout.SetColumnSpan(name, 2);

            var description = new Label
            {
                Text = viewModel.Description,
                Font = Palette.Ui(10f),
                ForeColor = Palette.Muted,
                Dock = DockStyle.Fill
            };
            layout.Controls.Add(description, 0, 1);
            layout.SetColumnSpan(description, 2);

            AddRow(layout, 2, "Interfaccia attiva", viewModel.UiName);
            AddRow(layout, 3, "Runtime", viewModel.Runtime);

            card.Controls.Add(layout);

            var title = new Label
            {
                Text = viewModel.Title,
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 44,
                Font = Palette.Ui(20f, FontStyle.Bold),
                ForeColor = Palette.Text
            };
            var gap = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = Palette.Background };

            Controls.Add(card);
            Controls.Add(gap);
            Controls.Add(title);
        }

        private static void AddRow(TableLayoutPanel layout, int row, string caption, string value)
        {
            layout.Controls.Add(new Label
            {
                Text = caption,
                Font = Palette.Ui(10f),
                ForeColor = Palette.Muted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, row);
            layout.Controls.Add(new Label
            {
                Text = value,
                Font = Palette.Ui(10f, FontStyle.Bold),
                ForeColor = Palette.Text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, row);
        }
    }
}
