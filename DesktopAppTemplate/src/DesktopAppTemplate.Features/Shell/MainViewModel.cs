using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Mvvm;

namespace DesktopAppTemplate.Features.Shell
{
    /// <summary>Finestra principale: menu laterale e pagina corrente.</summary>
    public sealed class MainViewModel : ObservableObject
    {
        private PageViewModel _selectedPage;

        public MainViewModel(IEnumerable<PageViewModel> pages, IAppInfo appInfo)
        {
            Pages = pages.OrderBy(p => p.Order).ToList();
            AppName = appInfo.Name;
            VersionText = "v" + appInfo.Version;

            NavigateCommand = new AsyncRelayCommand<PageViewModel>(NavigateAsync, page => page != null);
        }

        public string AppName { get; }
        public string VersionText { get; }

        public IReadOnlyList<PageViewModel> Pages { get; }

        public AsyncRelayCommand<PageViewModel> NavigateCommand { get; }

        public PageViewModel SelectedPage
        {
            get { return _selectedPage; }
            private set { SetProperty(ref _selectedPage, value); }
        }

        /// <summary>Mostra la prima pagina; da chiamare quando la finestra è visibile.</summary>
        public Task InitializeAsync() => NavigateCommand.ExecuteAsync(Pages.FirstOrDefault());

        private async Task NavigateAsync(PageViewModel page)
        {
            foreach (var p in Pages)
                p.IsSelected = ReferenceEquals(p, page);

            SelectedPage = page;
            await page.OnNavigatedToAsync();
        }
    }
}
