using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using DesktopAppTemplate.Core.Mvvm;
using DesktopAppTemplate.Features.About;
using DesktopAppTemplate.Features.Shell;
using DesktopAppTemplate.Features.Tasks;
using DesktopAppTemplate.UI.WinForms.Components;
using DesktopAppTemplate.UI.WinForms.Views;

namespace DesktopAppTemplate.UI.WinForms
{
    /// <summary>
    /// Finestra principale. La cornice (menu laterale scuro e area contenuto) è nel designer;
    /// le voci del menu e le pagine, che dipendono dai view model registrati, si creano qui.
    /// </summary>
    public partial class MainForm : Form
    {
        private MainViewModel _viewModel;
        private readonly Dictionary<PageViewModel, Control> _views = new Dictionary<PageViewModel, Control>();
        private readonly Dictionary<PageViewModel, NavButton> _navButtons = new Dictionary<PageViewModel, NavButton>();

        /// <summary>Costruttore per il designer; a runtime usare <see cref="MainForm(MainViewModel)"/>.</summary>
        public MainForm()
        {
            InitializeComponent();
        }

        public MainForm(MainViewModel viewModel)
            : this()
        {
            Bind(viewModel);
        }

        private void Bind(MainViewModel viewModel)
        {
            _viewModel = viewModel;

            Text = viewModel.AppName;
            brandNameLabel.Text = viewModel.AppName;
            versionLabel.Text = viewModel.VersionText;

            // Le voci si aggiungono dall'ultima alla prima: con Dock=Top l'ultima aggiunta sta in alto.
            for (var i = viewModel.Pages.Count - 1; i >= 0; i--)
            {
                var page = viewModel.Pages[i];

                var view = CreateView(page);
                view.Dock = DockStyle.Fill;
                view.Visible = false;
                contentPanel.Controls.Add(view);
                _views[page] = view;

                var button = new NavButton { Glyph = page.Icon, Text = page.Title, Dock = DockStyle.Top };
                var target = page;
                button.Click += (s, e) => viewModel.NavigateCommand.Execute(target);
                navPanel.Controls.Add(button);
                _navButtons[page] = button;

                page.PropertyChanged += OnPagePropertyChanged;
            }

            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            Disposed += (s, e) => Unbind();
        }

        private void Unbind()
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            foreach (var page in _views.Keys)
                page.PropertyChanged -= OnPagePropertyChanged;
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
