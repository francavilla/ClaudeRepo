using System;
using System.Linq;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Messaging;
using DesktopAppTemplate.Features.Tasks;
using DesktopAppTemplate.Features.Tasks.AddTask;
using DesktopAppTemplate.Features.Tasks.ListTasks;
using DesktopAppTemplate.Features.Tasks.RemoveTask;
using DesktopAppTemplate.Features.Tasks.ToggleTask;
using Xunit;

namespace DesktopAppTemplate.Tests
{
    public class TaskSliceTests
    {
        [Fact]
        public async Task AddTask_salva_il_titolo_ripulito_con_la_data_corrente()
        {
            var host = new TestHost();

            var item = await host.Mediator.Send(new AddTaskCommand("  Scrivere i test  "));

            Assert.Equal("Scrivere i test", item.Title);
            Assert.Equal(host.Clock.Now, item.CreatedAt);
            Assert.False(item.IsCompleted);
            Assert.Single(await host.Repository.GetAllAsync(default(System.Threading.CancellationToken)));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AddTask_rifiuta_un_titolo_vuoto(string title)
        {
            var host = new TestHost();

            await Assert.ThrowsAsync<ValidationException>(() => host.Mediator.Send(new AddTaskCommand(title)));
        }

        [Fact]
        public async Task AddTask_rifiuta_un_titolo_troppo_lungo()
        {
            var host = new TestHost();
            var title = new string('x', AddTaskValidator.MaxTitleLength + 1);

            await Assert.ThrowsAsync<ValidationException>(() => host.Mediator.Send(new AddTaskCommand(title)));
        }

        [Fact]
        public async Task ToggleTask_inverte_lo_stato()
        {
            var host = new TestHost();
            var item = await host.Mediator.Send(new AddTaskCommand("a"));

            var done = await host.Mediator.Send(new ToggleTaskCommand(item.Id));
            var undone = await host.Mediator.Send(new ToggleTaskCommand(item.Id));

            Assert.True(done.IsCompleted);
            Assert.False(undone.IsCompleted);
        }

        [Fact]
        public async Task ToggleTask_su_attivita_inesistente_solleva_eccezione()
        {
            var host = new TestHost();

            await Assert.ThrowsAsync<InvalidOperationException>(() => host.Mediator.Send(new ToggleTaskCommand(Guid.NewGuid())));
        }

        [Fact]
        public async Task RemoveTask_elimina_l_attivita()
        {
            var host = new TestHost();
            var item = await host.Mediator.Send(new AddTaskCommand("a"));

            await host.Mediator.Send(new RemoveTaskCommand(item.Id));

            var result = await host.Mediator.Send(new ListTasksQuery(TaskFilter.All));
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task ListTasks_applica_il_filtro_e_conta_il_totale()
        {
            var host = new TestHost();
            var first = await host.Mediator.Send(new AddTaskCommand("prima"));
            await host.Mediator.Send(new AddTaskCommand("seconda"));
            await host.Mediator.Send(new ToggleTaskCommand(first.Id));

            var all = await host.Mediator.Send(new ListTasksQuery(TaskFilter.All));
            var active = await host.Mediator.Send(new ListTasksQuery(TaskFilter.Active));
            var completed = await host.Mediator.Send(new ListTasksQuery(TaskFilter.Completed));

            Assert.Equal(2, all.Items.Count);
            Assert.Equal(new[] { "seconda" }, active.Items.Select(t => t.Title));
            Assert.Equal(new[] { "prima" }, completed.Items.Select(t => t.Title));
            Assert.Equal(2, completed.TotalCount);
            Assert.Equal(1, completed.CompletedCount);
        }
    }
}
