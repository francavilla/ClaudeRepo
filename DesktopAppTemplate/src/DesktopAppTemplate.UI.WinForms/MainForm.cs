using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DesktopAppTemplate.Core.Mvvm;
using DesktopAppTemplate.Features.About;
using DesktopAppTemplate.Features.Shell;
using DesktopAppTemplate.Features.Tasks;
using DesktopAppTemplate.UI.WinForms.Components;
using DesktopAppTemplate.UI.WinForms.Views;

namespace DesktopAppTemplate.UI.WinForms
{
    /// <summary>Finestra principale: menu laterale scuro e area contenuto a card, come nella versione WPF.</summary>
    internal sealed class MainForm : Form
    {
        private readonly MainViewModel _viewModel;
        private readonly Panel _content;
        private readonly Dictionary<PageViewModel, Control> _views = new Dictionary<PageViewModel, Control>();
        private readonly Dictionary<PageViewModel, NavButton> _navButtons = new Dictionary<PageViewModel, NavButton>();

        public MainForm(MainViewModel viewModel)
        {
            _viewModel = viewModel;

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Palette.Ui(9.5f);
            Text = viewModel.AppName;
            BackColor = Palette.Background;
            ClientSize = new Size(1100, 720);
            MinimumSize = new Size(900, 620);
            StartPosition = FormStartPosition.CenterScreen;

            _content = new Panel { Dock = DockStyle.Fill, BackColor = Palette.Background };

            var sidebar = new Panel { Dock = DockStyle.Left, Width = 240, BackColor = Palette.Sidebar };

            // Le voci si aggiungono dall'ultima alla prima: con Dock=Top l'ultima aggiunta sta in alto.
            for (var i = viewModel.Pages.Count - 1; i >= 0; i--)
            {
                var page = viewModel.Pages[i];

                var view = CreateView(page);
                view.Dock = DockStyle.Fill;
                view.Visible = false;
                _content.Controls.Add(view);
                _views[page] = view;

                var button = new NavButton(page.Icon, page.Title) { Dock = DockStyle.Top };
                var target = page;
                button.Click += (s, e) => viewModel.NavigateCommand.Execute(target);
                sidebar.Controls.Add(button);
                _navButtons[page] = button;

                page.PropertyChanged += OnPagePropertyChanged;
            }

            var spacer = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Palette.Sidebar };
            var brand = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = Palette.Sidebar };
            brand.Controls.Add(new Label
            {
                Text = viewModel.AppName,
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = Palette.Ui(13f, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(24, 0, 0, 2)
            });
            brand.Controls.Add(new Label
            {
                Text = "Applicazione desktop",
                Dock = DockStyle.Bottom,
                Height = 24,
                ForeColor = ColorTranslator.FromHtml("#9CA3AF"),
                Font = Palette.Ui(8.5f),
                Padding = new Padding(24, 0, 0, 0)
            });
            var version = new Label
            {
                Text = viewModel.VersionText,
                Dock = DockStyle.Bottom,
                Height = 40,
                ForeColor = ColorTranslator.FromHtml("#6B7280"),
                Font = Palette.Ui(8.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0)
            };

            sidebar.Controls.Add(spacer);
            sidebar.Controls.Add(brand);
            sidebar.Controls.Add(version);

            // Il controllo "Fill" si aggiunge per primo.
            Controls.Add(_content);
            Controls.Add(sidebar);

            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        /// <summary>Pagina (view model) -> view. Una riga per ogni nuova pagina.</summary>
        private static Control CreateView(PageViewModel page)
        {
            var tasks = page as TaskListViewModel;
            if (tasks != null)
                return new TaskListView(tasks);

            var about = page as AboutViewModel;
            if (about != null)
                return new AboutView(about);

            throw new NotSupportedException("Nessuna view Windows Forms per la pagina " + page.GetType().Name + ".");
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MainViewModel.SelectedPage)) return;

            foreach (var pair in _views)
                pair.Value.Visible = ReferenceEquals(pair.Key, _viewModel.SelectedPage);
        }

        private void OnPagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(PageViewModel.IsSelected)) return;

            var page = (PageViewModel)sender;
            _navButtons[page].Selected = page.IsSelected;
        }
    }
}
