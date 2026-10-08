using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Data;
using DesktopAppTemplate.Data;
using DesktopAppTemplate.Data.Tasks;
using DesktopAppTemplate.Data.Dal;
using DesktopAppTemplate.Features.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DesktopAppTemplate.Tests
{
    /// <summary>
    /// Finta DAL: una connessione unica e già aperta (come il singleton della libreria vera), transazioni esplicite,
    /// registro delle chiamate e rilevamento di accessi sovrapposti.
    /// </summary>
    internal sealed class FakeDalGateway : IDalGateway, IDisposable
    {
        private readonly DbConnection _connection;
        private DbTransaction _transaction;
        private int _running;

        public FakeDalGateway(IDbConnectionFactory connections)
        {
            _connection = connections.CreateConnection();
            _connection.Open();
        }

        public List<string> Calls { get; } = new List<string>();
        public List<IReadOnlyList<KeyValuePair<string, object>>> Parameters { get; } = new List<IReadOnlyList<KeyValuePair<string, object>>>();
        public int MaxConcurrentCalls { get; private set; }

        public IDataReader ExecuteReader(string sql, IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            return Track("reader", sql, parameters, command => command.ExecuteReader());
        }

        public int ExecuteNonQuery(string sql, IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            return Track("nonquery", sql, parameters, command => command.ExecuteNonQuery());
        }

        public void BeginTransaction()
        {
            Calls.Add("begin");
            _transaction = _connection.BeginTransaction();
        }

        public void Commit()
        {
            Calls.Add("commit");
            _transaction.Commit();
            _transaction = null;
        }

        public void Rollback()
        {
            Calls.Add("rollback");
            _transaction.Rollback();
            _transaction = null;
        }

        public void Dispose() => _connection.Dispose();

        private T Track<T>(string kind, string sql, IReadOnlyList<KeyValuePair<string, object>> parameters, Func<DbCommand, T> run)
        {
            var running = Interlocked.Increment(ref _running);
            try
            {
                lock (Calls)
                {
                    MaxConcurrentCalls = Math.Max(MaxConcurrentCalls, running);
                    Calls.Add(kind);
                    Parameters.Add(parameters);
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.Transaction = _transaction;
                    foreach (var p in parameters)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = p.Key;
                        parameter.Value = p.Value ?? DBNull.Value;
                        command.Parameters.Add(parameter);
                    }

                    // Un'operazione lenta: se l'adattatore non serializzasse, altre chiamate si sovrapporrebbero qui.
                    Thread.Sleep(5);
                    return run(command);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _running);
            }
        }
    }

    /// <summary>I repository ADO.NET sopra l'adattatore DAL devono comportarsi come con le altre tecnologie.</summary>
    public class DalTaskRepositoryTests : TaskRepositoryContractTests
    {
        internal override ITaskRepository CreateRepository(IDbConnectionFactory connections)
        {
            var gateway = new FakeDalGateway(connections);
            return new AdoNetTaskRepository(new DalExecutor(gateway, new SqlDialect(), new DalLock()));
        }
    }

    public class DalAdapterTests : IDisposable
    {
        private static readonly CancellationToken None = default(CancellationToken);

        private readonly SqliteTestDatabase _database = new SqliteTestDatabase();
        private readonly FakeDalGateway _gateway;

        public DalAdapterTests()
        {
            _gateway = new FakeDalGateway(_database.Connections);
        }

        public void Dispose()
        {
            _gateway.Dispose();
            _database.Dispose();
        }

        private AdoNetTaskRepository CreateRepository(DalLock dalLock = null, ISqlDialect dialect = null)
            => new AdoNetTaskRepository(new DalExecutor(_gateway, dialect ?? new SqlDialect(), dalLock ?? new DalLock()));

        private static TaskItem NewTask(string title) => new TaskItem(Guid.NewGuid(), title, new DateTime(2026, 10, 8, 9, 0, 0, DateTimeKind.Local));

        [Fact]
        public async Task Le_operazioni_sulla_connessione_singleton_sono_serializzate()
        {
            var repository = CreateRepository();

            await Task.WhenAll(Enumerable.Range(0, 12).Select(i => repository.AddAsync(NewTask("t" + i), None)));

            Assert.Equal(1, _gateway.MaxConcurrentCalls);
            Assert.Equal(12, (await repository.GetAllAsync(None)).Count);
        }

        [Fact]
        public async Task I_parametri_arrivano_a_DAL_con_il_prefisso_del_dialetto()
        {
            await CreateRepository(dialect: new SqlDialect(":")).AddAsync(NewTask("x"), None);

            var names = _gateway.Parameters.Last().Select(p => p.Key).ToList();
            Assert.Equal(new[] { ":Id", ":Title", ":CreatedAt", ":IsCompleted" }, names);
        }

        [Fact]
        public async Task La_transazione_conferma_se_il_lavoro_riesce()
        {
            var dalLock = new DalLock();
            var repository = CreateRepository(dalLock);
            var runner = new DalTransactionRunner(_gateway, dalLock);

            await runner.RunAsync(async ct =>
            {
                await repository.AddAsync(NewTask("a"), ct);
                await repository.AddAsync(NewTask("b"), ct);
            });

            Assert.Equal(new[] { "begin", "nonquery", "nonquery", "commit" }, _gateway.Calls);
            Assert.Equal(2, (await CreateRepository().GetAllAsync(None)).Count);
        }

        [Fact]
        public async Task La_transazione_annulla_e_rilancia_l_errore_originale()
        {
            var dalLock = new DalLock();
            var repository = CreateRepository(dalLock);
            var runner = new DalTransactionRunner(_gateway, dalLock);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(async ct =>
            {
                await repository.AddAsync(NewTask("a"), ct);
                throw new InvalidOperationException("errore del lavoro");
            }));

            Assert.Equal("errore del lavoro", ex.Message);
            Assert.Equal(new[] { "begin", "nonquery", "rollback" }, _gateway.Calls);
            Assert.Empty(await CreateRepository().GetAllAsync(None));
        }

        [Fact]
        public async Task Dentro_la_transazione_le_operazioni_non_si_bloccano_da_sole_e_le_altre_attendono()
        {
            var dalLock = new DalLock();
            var repository = CreateRepository(dalLock);
            var runner = new DalTransactionRunner(_gateway, dalLock);
            var insideStarted = new TaskCompletionSource<bool>();
            var release = new TaskCompletionSource<bool>();

            var transaction = runner.RunAsync(async ct =>
            {
                await repository.AddAsync(NewTask("dentro"), ct);   // non deve andare in stallo
                insideStarted.SetResult(true);
                await release.Task;
            });
            await insideStarted.Task;

            var outside = repository.AddAsync(NewTask("fuori"), None);
            await Task.Delay(100);
            Assert.False(outside.IsCompleted);   // attende la fine della transazione

            release.SetResult(true);
            var all = Task.WhenAll(transaction, outside);
            Assert.Same(all, await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(10))));

            Assert.Equal(new[] { "begin", "nonquery", "commit", "nonquery" }, _gateway.Calls);
        }

        [Fact]
        public async Task Una_transazione_annidata_partecipa_a_quella_esterna_senza_aprirne_un_altra()
        {
            var dalLock = new DalLock();
            var repository = CreateRepository(dalLock);
            var runner = new DalTransactionRunner(_gateway, dalLock);

            await runner.RunAsync(ct => runner.RunAsync(inner => repository.AddAsync(NewTask("x"), inner), ct));

            Assert.Equal(new[] { "begin", "nonquery", "commit" }, _gateway.Calls);
        }

        [Fact]
        public async Task Il_gateway_predefinito_segnala_che_DAL_non_e_collegata()
        {
            var services = new ServiceCollection();
            services.AddDatabase(DatabaseProvider.Sqlite, _database.Connections.ConnectionString);
            services.AddDalAdapter();
            services.AddAdoNetData();

            var repository = services.BuildServiceProvider().GetRequiredService<ITaskRepository>();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetAllAsync(None));
            Assert.Contains("DalGateway", ex.Message);
        }

        [Fact]
        public void AddDalAdapter_sostituisce_l_executor_predefinito_e_registra_le_transazioni()
        {
            var services = new ServiceCollection();
            services.AddDatabase(DatabaseProvider.Sqlite, _database.Connections.ConnectionString);
            services.AddDalAdapter<FakeGatewayForDi>();
            var provider = services.BuildServiceProvider();

            Assert.IsType<DalExecutor>(provider.GetRequiredService<IDbExecutor>());
            Assert.IsType<DalTransactionRunner>(provider.GetRequiredService<ITransactionRunner>());
        }

        private sealed class FakeGatewayForDi : IDalGateway
        {
            public IDataReader ExecuteReader(string sql, IReadOnlyList<KeyValuePair<string, object>> parameters) => throw new NotSupportedException();
            public int ExecuteNonQuery(string sql, IReadOnlyList<KeyValuePair<string, object>> parameters) => throw new NotSupportedException();
            public void BeginTransaction() => throw new NotSupportedException();
            public void Commit() => throw new NotSupportedException();
            public void Rollback() => throw new NotSupportedException();
        }
    }
}
