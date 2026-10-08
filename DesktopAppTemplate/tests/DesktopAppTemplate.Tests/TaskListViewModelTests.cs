using System.Threading.Tasks;
using DesktopAppTemplate.Features.Tasks;
using Xunit;

namespace DesktopAppTemplate.Tests
{
    public class TaskListViewModelTests
    {
        private static TaskListViewModel Create(TestHost host, FakeDialogService dialogs = null)
        {
            return new TaskListViewModel(host.Mediator, dialogs ?? new FakeDialogService());
        }

        [Fact]
        public async Task Add_aggiunge_la_riga_e_svuota_la_casella()
        {
            var host = new TestHost();
            var vm = Create(host);
            await vm.OnNavigatedToAsync();

            vm.NewTitle = "Nuova";
            await vm.AddCommand.ExecuteAsync();

            Assert.Single(vm.Items);
            Assert.Equal("Nuova", vm.Items[0].Title);
            Assert.Equal(string.Empty, vm.NewTitle);
            Assert.False(vm.IsEmpty);
            Assert.False(vm.HasError);
        }

        [Fact]
        public void Add_e_disabilitato_con_titolo_vuoto()
        {
            var vm = Create(new TestHost());

            Assert.False(vm.AddCommand.CanExecute(null));

            vm.NewTitle = "x";
            Assert.True(vm.AddCommand.CanExecute(null));
        }

        [Fact]
        public async Task Errore_di_validazione_viene_mostrato_senza_eccezioni()
        {
            var host = new TestHost();
            var vm = Create(host);

            vm.NewTitle = new string('x', 500);
            await vm.AddCommand.ExecuteAsync();

            Assert.True(vm.HasError);
            Assert.Empty(vm.Items);
        }

        [Fact]
        public async Task Remove_chiede_conferma_e_rispetta_il_rifiuto()
        {
            var host = new TestHost();
            var dialogs = new FakeDialogService { ConfirmResult = false };
            var vm = Create(host, dialogs);
            vm.NewTitle = "a";
            await vm.AddCommand.ExecuteAsync();

            await vm.RemoveCommand.ExecuteAsync(vm.Items[0]);

            Assert.Single(vm.Items);
        }

        [Fact]
        public async Task Toggle_e_filtro_aggiornano_l_elenco_e_il_riepilogo()
        {
            var host = new TestHost();
            var vm = Create(host);
            vm.NewTitle = "a";
            await vm.AddCommand.ExecuteAsync();

            await vm.ToggleCommand.ExecuteAsync(vm.Items[0]);
            Assert.Equal("1 attività · 1 completate", vm.Summary);

            vm.SelectedFilter = vm.FilterOptions[1]; // "Da fare"
            await Task.Yield();
            Assert.Empty(vm.Items);
            Assert.True(vm.IsEmpty);
        }
    }
}
