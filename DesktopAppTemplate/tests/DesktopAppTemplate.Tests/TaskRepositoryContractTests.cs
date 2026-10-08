using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Data;
using DesktopAppTemplate.Data;
using DesktopAppTemplate.Data.Ado;
using DesktopAppTemplate.Data.Dapper;
using DesktopAppTemplate.Data.EntityFramework;
using DesktopAppTemplate.Features.Tasks;
using Xunit;

namespace DesktopAppTemplate.Tests
{
    /// <summary>
    /// Stesse verifiche per ogni tecnologia di accesso (ADO.NET, Dapper, Entity Framework): devono comportarsi
    /// in modo identico, perché le slice non devono accorgersi di quale è in uso.
    /// </summary>
    public abstract class TaskRepositoryContractTests : IDisposable
    {
        private static readonly CancellationToken None = default(CancellationToken);

        private readonly SqliteTestDatabase _database = new SqliteTestDatabase();

        internal IDbConnectionFactory Connections => _database.Connections;

        internal abstract ITaskRepository CreateRepository(IDbConnectionFactory connections);

        public void Dispose() => _database.Dispose();

        private static TaskItem NewTask(string title, DateTime? createdAt = null, bool completed = false)
        {
            return new TaskItem(Guid.NewGuid(), title, createdAt ?? new DateTime(2026, 10, 8, 9, 30, 15, DateTimeKind.Local), completed);
        }

        [Fact]
        public async Task Add_poi_GetAll_restituisce_gli_stessi_dati()
        {
            var task = NewTask("Prima attività", completed: true);
            var repository = CreateRepository(Connections);

            await repository.AddAsync(task, None);
            var all = await repository.GetAllAsync(None);

            var item = Assert.Single(all);
            Assert.Equal(task.Id, item.Id);
            Assert.Equal("Prima attività", item.Title);
            Assert.Equal(task.CreatedAt, item.CreatedAt);
            Assert.True(item.IsCompleted);
        }

        [Fact]
        public async Task Get_restituisce_l_elemento_oppure_null()
        {
            var task = NewTask("Cercami");
            var repository = CreateRepository(Connections);
            await repository.AddAsync(task, None);

            var found = await repository.GetAsync(task.Id, None);
            var missing = await repository.GetAsync(Guid.NewGuid(), None);

            Assert.Equal("Cercami", found.Title);
            Assert.Null(missing);
        }

        [Fact]
        public async Task Update_modifica_i_dati()
        {
            var task = NewTask("Da completare");
            var repository = CreateRepository(Connections);
            await repository.AddAsync(task, None);

            await repository.UpdateAsync(task.WithCompleted(true), None);

            Assert.True((await repository.GetAsync(task.Id, None)).IsCompleted);
        }

        [Fact]
        public async Task Remove_elimina_l_elemento_e_ignora_gli_inesistenti()
        {
            var keep = NewTask("Resta");
            var remove = NewTask("Va via");
            var repository = CreateRepository(Connections);
            await repository.AddAsync(keep, None);
            await repository.AddAsync(remove, None);

            await repository.RemoveAsync(remove.Id, None);
            await repository.RemoveAsync(Guid.NewGuid(), None);

            var all = await repository.GetAllAsync(None);
            Assert.Equal(new[] { keep.Id }, all.Select(t => t.Id));
        }

        [Fact]
        public async Task Titoli_con_apici_accenti_e_simboli_si_salvano_senza_alterazioni()
        {
            var title = "L'attività \"città\" — 100% [OK]; DROP TABLE Tasks;--";
            var task = NewTask(title);
            var repository = CreateRepository(Connections);

            await repository.AddAsync(task, None);

            Assert.Equal(title, (await repository.GetAsync(task.Id, None)).Title);
            Assert.Single(await repository.GetAllAsync(None));
        }

        [Fact]
        public async Task Le_attivita_sono_restituite_in_ordine_di_creazione()
        {
            var older = NewTask("vecchia", new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc));
            var newer = NewTask("nuova", new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc));
            var repository = CreateRepository(Connections);
            await repository.AddAsync(newer, None);
            await repository.AddAsync(older, None);

            var all = await repository.GetAllAsync(None);

            Assert.Equal(new[] { "vecchia", "nuova" }, all.Select(t => t.Title));
        }

        [Fact]
        public async Task I_dati_sono_condivisi_tra_istanze_dello_stesso_repository()
        {
            var task = NewTask("Condivisa");
            await CreateRepository(Connections).AddAsync(task, None);

            var reloaded = await CreateRepository(Connections).GetAllAsync(None);

            Assert.Equal(task.Id, Assert.Single(reloaded).Id);
        }
    }

    public class AdoNetTaskRepositoryTests : TaskRepositoryContractTests
    {
        internal override ITaskRepository CreateRepository(IDbConnectionFactory connections)
            => new AdoNetTaskRepository(new AdoNetExecutor(connections, new SqlDialect()));
    }

    public class DapperTaskRepositoryTests : TaskRepositoryContractTests
    {
        internal override ITaskRepository CreateRepository(IDbConnectionFactory connections)
            => new DapperTaskRepository(connections, new SqlDialect());
    }

    public class EntityFrameworkTaskRepositoryTests : TaskRepositoryContractTests
    {
        internal override ITaskRepository CreateRepository(IDbConnectionFactory connections)
            => new EntityFrameworkTaskRepository(connections);
    }

    /// <summary>Le tre tecnologie leggono e scrivono le stesse tabelle: i dati scritti da una si leggono con le altre.</summary>
    public class CrossTechnologyTests : IDisposable
    {
        private static readonly CancellationToken None = default(CancellationToken);

        private readonly SqliteTestDatabase _database = new SqliteTestDatabase();

        public void Dispose() => _database.Dispose();

        [Fact]
        public async Task I_dati_scritti_con_una_tecnologia_si_leggono_con_le_altre()
        {
            var connections = _database.Connections;
            var ado = new AdoNetTaskRepository(new AdoNetExecutor(connections, new SqlDialect()));
            var dapper = new DapperTaskRepository(connections, new SqlDialect());
            var ef = new EntityFrameworkTaskRepository(connections);
            var task = new TaskItem(Guid.NewGuid(), "Scritta con ADO.NET", new DateTime(2026, 10, 8, 9, 30, 15, DateTimeKind.Local));

            await ado.AddAsync(task, None);
            Assert.Equal("Scritta con ADO.NET", (await dapper.GetAsync(task.Id, None)).Title);
            Assert.Equal("Scritta con ADO.NET", (await ef.GetAsync(task.Id, None)).Title);

            await ef.UpdateAsync(task.WithCompleted(true), None);
            Assert.True((await ado.GetAsync(task.Id, None)).IsCompleted);
            Assert.True((await dapper.GetAsync(task.Id, None)).IsCompleted);

            await dapper.RemoveAsync(task.Id, None);
            Assert.Null(await ado.GetAsync(task.Id, None));
            Assert.Empty(await ef.GetAllAsync(None));
        }
    }
}
