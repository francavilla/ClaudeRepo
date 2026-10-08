using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Configuration;
using DesktopAppTemplate.Core.Data;
using DesktopAppTemplate.Data;
using DesktopAppTemplate.Data.Tasks;
using DesktopAppTemplate.Features.Tasks;
using DesktopAppTemplate.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DesktopAppTemplate.Tests
{
    public class DataInfrastructureTests
    {
        private static readonly CancellationToken None = default(CancellationToken);

        // --- Dialetto ---

        [Fact]
        public void Il_dialetto_predefinito_non_cambia_il_testo()
        {
            var dialect = new SqlDialect();

            Assert.Equal("@", dialect.ParameterPrefix);
            Assert.Equal("SELECT * FROM T WHERE Id = @Id", dialect.Adapt("SELECT * FROM T WHERE Id = @Id"));
        }

        [Fact]
        public void Un_altro_prefisso_riscrive_i_segnaposto()
        {
            var dialect = new SqlDialect(":");

            Assert.Equal("INSERT INTO T (A, B) VALUES (:A, :B)", dialect.Adapt("INSERT INTO T (A, B) VALUES (@A, @B)"));
        }

        [Fact]
        public void Il_prefisso_e_obbligatorio()
        {
            Assert.Throws<ArgumentException>(() => new SqlDialect(""));
        }

        // --- Fabbrica di connessioni ---

        [Fact]
        public async Task La_fabbrica_SQLite_crea_la_cartella_del_file()
        {
            using (var database = new SqliteTestDatabase(migrate: false))
            {
                var folder = Path.GetDirectoryName(database.FilePath);
                Assert.False(Directory.Exists(folder));

                using (var connection = await database.Connections.OpenConnectionAsync(None))
                    Assert.Equal(ConnectionState.Open, connection.State);

                Assert.True(Directory.Exists(folder));
            }
        }

        [Fact]
        public void La_stringa_di_connessione_e_obbligatoria()
        {
            Assert.Throws<ArgumentException>(() => new DbConnectionFactory(DatabaseProvider.Sqlite, " "));
        }

        // --- Migrazioni ---

        [Fact]
        public async Task La_migrazione_crea_lo_schema_una_sola_volta()
        {
            using (var database = new SqliteTestDatabase(migrate: false))
            {
                var migrator = new DatabaseMigrator(database.Connections, new SqlDialect());

                var first = await migrator.MigrateAsync();
                var second = await migrator.MigrateAsync();

                Assert.True(first.IsNewDatabase);
                Assert.Equal(2, first.AppliedCount);
                Assert.False(second.IsNewDatabase);
                Assert.Equal(0, second.AppliedCount);
                Assert.Equal(2, second.PreviousVersion);

                var executor = new AdoNetExecutor(database.Connections, new SqlDialect());
                var tables = await executor.QueryAsync("SELECT name FROM sqlite_master WHERE type = 'table'", null, r => r.GetString(0));
                Assert.Contains("Tasks", tables);
                Assert.Contains("Log", tables);
                Assert.Contains("SchemaVersion", tables);

                var versions = await executor.QueryAsync("SELECT Version FROM SchemaVersion", null, r => r.GetInt64(0));
                Assert.Equal(new long[] { 1, 2 }, versions);
            }
        }

        // --- Executor ADO.NET e punti di aggancio ---

        [Fact]
        public async Task L_executor_esegue_con_un_dialetto_diverso()
        {
            using (var database = new SqliteTestDatabase())
            {
                var executor = new AdoNetExecutor(database.Connections, new SqlDialect(":"));
                var row = TaskRowMapper.ToRow(new TaskItem(Guid.NewGuid(), "Con i due punti", DateTime.Now));

                var affected = await executor.ExecuteAsync(TaskSql.Insert, TaskRowMapper.ToParameters(row));
                var rows = await executor.QueryAsync(TaskSql.SelectById, TaskRowMapper.IdParameter(Guid.Parse(row.Id)), TaskRowMapper.FromRecord);

                Assert.Equal(1, affected);
                Assert.Equal("Con i due punti", Assert.Single(rows).Title);
            }
        }

        [Fact]
        public async Task Il_gancio_sui_comandi_viene_chiamato_con_il_testo_adattato_e_il_timeout()
        {
            using (var database = new SqliteTestDatabase())
            {
                var seen = new List<string>();
                var options = new AdoNetOptions
                {
                    CommandTimeoutSeconds = 7,
                    ConfigureCommand = command => seen.Add(command.CommandTimeout + "|" + command.CommandText)
                };
                var executor = new AdoNetExecutor(database.Connections, new SqlDialect(), options);

                await executor.QueryAsync("SELECT COUNT(*) FROM Tasks WHERE IsCompleted = @Done", new Dictionary<string, object> { { "Done", 0L } }, r => r.GetInt64(0));

                Assert.Equal(new[] { "7|SELECT COUNT(*) FROM Tasks WHERE IsCompleted = @Done" }, seen);
            }
        }

        private sealed class RecordingExecutor : IDbExecutor
        {
            public List<string> Sql { get; } = new List<string>();
            public List<IDictionary<string, object>> Parameters { get; } = new List<IDictionary<string, object>>();

            public Task<IReadOnlyList<T>> QueryAsync<T>(string sql, IDictionary<string, object> parameters, Func<IDataRecord, T> map,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                Sql.Add(sql);
                Parameters.Add(parameters);
                return Task.FromResult((IReadOnlyList<T>)new List<T>());
            }

            public Task<int> ExecuteAsync(string sql, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default(CancellationToken))
            {
                Sql.Add(sql);
                Parameters.Add(parameters);
                return Task.FromResult(1);
            }
        }

        [Fact]
        public async Task Il_repository_ADO_usa_solo_l_executor_quindi_si_puo_agganciare_una_libreria()
        {
            var executor = new RecordingExecutor();
            var repository = new AdoNetTaskRepository(executor);
            var task = new TaskItem(Guid.NewGuid(), "Titolo", new DateTime(2026, 10, 8, 9, 0, 0, DateTimeKind.Local));

            await repository.AddAsync(task, None);
            await repository.RemoveAsync(task.Id, None);
            var missing = await repository.GetAsync(task.Id, None);

            Assert.Equal(new[] { TaskSql.Insert, TaskSql.Delete, TaskSql.SelectById }, executor.Sql);
            Assert.Equal(new[] { "Id", "Title", "CreatedAt", "IsCompleted" }, executor.Parameters[0].Keys);
            Assert.Equal(task.Id.ToString("D"), executor.Parameters[1]["Id"]);
            Assert.Null(missing);
        }

        [Fact]
        public async Task Registrare_un_executor_proprio_dopo_AddDatabase_sostituisce_quello_predefinito()
        {
            using (var database = new SqliteTestDatabase(migrate: false))
            {
                var custom = new RecordingExecutor();
                var services = new ServiceCollection();
                services.AddDatabase(DatabaseProvider.Sqlite, database.Connections.ConnectionString);
                services.AddSingleton<IDbExecutor>(custom);   // aggancio: l'ultima registrazione vince
                services.AddAdoNetData();

                var repository = services.BuildServiceProvider().GetRequiredService<ITaskRepository>();
                await repository.GetAllAsync(None);

                Assert.Equal(new[] { TaskSql.SelectAll }, custom.Sql);
            }
        }

        // --- Impostazioni di archiviazione ---

        private static StorageSettings Settings(params KeyValuePair<string, string>[] values)
        {
            var built = new ConfigurationBuilder(StorageSettings.Options).AddSource("test", values).Build();
            Assert.Empty(built.Errors);
            return StorageSettings.From(built.Configuration);
        }

        private static KeyValuePair<string, string> P(string key, string value) => new KeyValuePair<string, string>(key, value);

        [Fact]
        public void I_valori_predefiniti_sono_SQL_Server_locale_con_ADO()
        {
            var settings = Settings();

            Assert.Equal(StorageKind.SqlServer, settings.Storage);
            Assert.Equal(DataAccessKind.Ado, settings.DataAccess);
            Assert.Equal(DatabaseProvider.SqlServer, settings.Provider);
            Assert.Equal("Server=.;Database=ClaudeDB;Integrated Security=True", settings.ResolveConnectionString("MiaApp"));
        }

        [Theory]
        [InlineData("SQLite", StorageKind.Sqlite)]
        [InlineData("sqlserver", StorageKind.SqlServer)]
        public void Storage_viene_letto_dalla_configurazione(string value, StorageKind expected)
        {
            Assert.Equal(expected, Settings(P("Storage", value), P("ConnectionString", "Server=x")).Storage);
        }

        [Theory]
        [InlineData("ado", DataAccessKind.Ado)]
        [InlineData("dal", DataAccessKind.Dal)]
        public void DataAccess_viene_letto_dalla_configurazione(string value, DataAccessKind expected)
        {
            Assert.Equal(expected, Settings(P("DataAccess", value)).DataAccess);
        }

        [Fact]
        public void Valori_non_ammessi_sono_segnalati_dalla_configurazione()
        {
            var built = new ConfigurationBuilder(StorageSettings.Options)
                .AddSource(ConfigurationBuilder.CommandLineSourceName, new[] { P("Storage", "oracle"), P("DataAccess", "dapper") })
                .Build();

            Assert.Equal(2, built.Errors.Count);
            Assert.Contains(built.Errors, e => e.Contains("--storage"));
            Assert.Contains(built.Errors, e => e.Contains("--data-access"));
        }

        [Fact]
        public void Con_SQL_Server_la_connessione_configurata_vince_su_quella_predefinita()
        {
            Assert.Equal(StorageSettings.DefaultSqlServerConnectionString, Settings(P("Storage", "sqlserver")).ResolveConnectionString("MiaApp"));
            Assert.Equal("Server=SRV;Database=Altro;Integrated Security=True",
                Settings(P("ConnectionString", "Server=SRV;Database=Altro;Integrated Security=True")).ResolveConnectionString("MiaApp"));
        }

        [Fact]
        public void La_connessione_SQLite_predefinita_e_un_file_nella_cartella_dei_dati()
        {
            var settings = Settings(P("Storage", "sqlite"), P("DataFolder", @"C:\Dati"));

            Assert.Equal(@"Data Source=C:\Dati\MiaApp.db", settings.ResolveConnectionString("MiaApp"));
        }

        [Fact]
        public void La_stringa_di_connessione_configurata_ha_la_precedenza_e_espande_le_variabili()
        {
            var settings = Settings(P("ConnectionString", @"Data Source=%TEMP%\x.db"));

            Assert.Equal("Data Source=" + Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), "x.db"), settings.ResolveConnectionString("MiaApp"));
        }

        [Fact]
        public void I_nomi_da_riga_di_comando_sono_quelli_attesi()
        {
            Assert.Equal(
                new[] { "--data-folder", "--storage", "--data-access", "--connection-string", "--no-migrate" },
                StorageSettings.Options.Select(o => o.CommandLineName));
        }

        [Fact]
        public void La_cartella_predefinita_sta_in_LocalAppData_con_il_nome_dell_app()
        {
            var folder = new StorageSettings(null).ResolveFolder("MiaApp");

            Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiaApp"), folder);
        }

        [Fact]
        public void La_cartella_configurata_espande_le_variabili_d_ambiente()
        {
            var folder = new StorageSettings(@"%TEMP%\MieiDati").ResolveFolder("MiaApp");

            Assert.Equal(Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), "MieiDati"), folder);
        }

        // --- --no-migrate ---

        [Fact]
        public void NoMigrate_e_un_flag_disattivato_per_impostazione_predefinita()
        {
            Assert.False(Settings().NoMigrate);
            Assert.True(Settings(P("NoMigrate", "true")).NoMigrate);
            Assert.Contains("--no-migrate", StorageSettings.Options.Select(o => o.CommandLineName));
            Assert.True(StorageSettings.NoMigrateOption.IsFlag);
        }

        [Fact]
        public void Da_riga_di_comando_il_flag_no_migrate_si_attiva_senza_valore()
        {
            var commandLine = CommandLineParser.Parse(new[] { "--no-migrate" }, StorageSettings.Options);
            var built = new ConfigurationBuilder(StorageSettings.Options)
                .AddSource(ConfigurationBuilder.CommandLineSourceName, commandLine.Values)
                .Build();

            Assert.Empty(commandLine.Errors);
            Assert.True(StorageSettings.From(built.Configuration).NoMigrate);
        }

        [Fact]
        public async Task Il_controllo_dello_schema_segnala_chiaramente_le_tabelle_mancanti()
        {
            using (var database = new SqliteTestDatabase(migrate: false))
            {
                var executor = new AdoNetExecutor(database.Connections, new SqlDialect());

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => SchemaVerifier.VerifyAsync(executor));

                Assert.Contains("--no-migrate", ex.Message);
                Assert.Contains("Tasks", ex.Message);
                Assert.NotNull(ex.InnerException);
            }
        }

        [Fact]
        public async Task Il_controllo_dello_schema_passa_se_le_tabelle_esistono_e_non_modifica_nulla()
        {
            using (var database = new SqliteTestDatabase())
            {
                var executor = new AdoNetExecutor(database.Connections, new SqlDialect());

                await SchemaVerifier.VerifyAsync(executor);

                var rows = await executor.QueryAsync("SELECT COUNT(*) FROM Tasks", null, r => r.GetInt64(0));
                Assert.Equal(0L, rows.Single());
            }
        }

        // --- Creazione del database (SQL Server) ---

        [Fact]
        public async Task La_creazione_del_database_non_fa_nulla_con_SQLite()
        {
            await DatabaseCreator.EnsureExistsAsync(new DbConnectionFactory(DatabaseProvider.Sqlite, "Data Source=non-serve.db"));
        }

        [Fact]
        public async Task Senza_nome_del_database_nella_connessione_non_si_tenta_nessuna_creazione()
        {
            // Nessun collegamento: se ci provasse, il server inesistente farebbe fallire il test.
            await DatabaseCreator.EnsureExistsAsync(new DbConnectionFactory(DatabaseProvider.SqlServer, "Server=server-inesistente;Integrated Security=True;Connect Timeout=1"));
        }

        [Fact]
        public async Task Se_il_server_non_risponde_l_errore_dice_database_server_e_come_procedere()
        {
            var factory = new DbConnectionFactory(DatabaseProvider.SqlServer, "Server=127.0.0.1,1;Database=ClaudeDB;Integrated Security=True;Connect Timeout=1");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseCreator.EnsureExistsAsync(factory));

            Assert.Contains("ClaudeDB", ex.Message);
            Assert.Contains("127.0.0.1", ex.Message);
            Assert.Contains("--no-migrate", ex.Message);
            Assert.Contains("--storage sqlite", ex.Message);
            Assert.NotNull(ex.InnerException);
        }
    }
}
