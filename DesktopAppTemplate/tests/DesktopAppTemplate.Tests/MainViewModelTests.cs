using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Mvvm;
using DesktopAppTemplate.Features.Shell;
using Xunit;

namespace DesktopAppTemplate.Tests
{
    public class MainViewModelTests
    {
        private sealed class FakeAppInfo : IAppInfo
        {
            public string Name => "MiaApp";
            public string Version => "2.3.4";
            public string Description => string.Empty;
            public string RuntimeDescription => string.Empty;
        }

        private sealed class TestPage : PageViewModel
        {
            public TestPage(string title, int order)
                : base(title, "", order)
            {
            }
        }

        [Fact]
        public void Il_titolo_della_finestra_contiene_nome_e_versione()
        {
            var main = new MainViewModel(new PageViewModel[0], new FakeAppInfo());

            Assert.Equal("MiaApp v2.3.4", main.WindowTitle);
            Assert.Equal("v2.3.4", main.VersionText);
        }

        [Fact]
        public async System.Threading.Tasks.Task Le_pagine_sono_ordinate_e_la_navigazione_evidenzia_quella_attiva()
        {
            var second = new TestPage("B", 20);
            var first = new TestPage("A", 10);
            var main = new MainViewModel(new PageViewModel[] { second, first }, new FakeAppInfo());

            await main.InitializeAsync();

            Assert.Same(first, main.Pages[0]);
            Assert.Same(first, main.SelectedPage);
            Assert.True(first.IsSelected);

            await main.NavigateCommand.ExecuteAsync(second);

            Assert.Same(second, main.SelectedPage);
            Assert.True(second.IsSelected);
            Assert.False(first.IsSelected);
        }
    }
}
