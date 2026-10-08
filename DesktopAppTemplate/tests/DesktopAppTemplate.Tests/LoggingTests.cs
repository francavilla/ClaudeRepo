using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core;
using DesktopAppTemplate.Core.Configuration;
using DesktopAppTemplate.Core.Data;
using DesktopAppTemplate.Core.Messaging;
using DesktopAppTemplate.Data;
using DesktopAppTemplate.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DesktopAppTemplate.Tests
{
    public class LoggingTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "dat-log-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_folder))
                    Directory.Delete(_folder, true);
            }
            catch (IOException)
            {
            }
        }

        private string ReadAllLogFiles()
        {
            return Directory.Exists(_folder)
                ? string.Concat(Directory.GetFiles(_folder, "*.log").OrderBy(f => f).Select(File.ReadAllText))
                : string.Empty;
        }

        // --- Impostazioni ---

        [Theory]
        [InlineData("file", LogTargets.File)]
        [InlineData("db", LogTargets.Database)]
        [InlineData("database", LogTargets.Database)]
        [InlineData("file,db", LogTargets.File | LogTargets.Database)]
        [InlineData("DB; File", LogTargets.File | LogTargets.Database)]
        [InlineData("none", LogTargets.None)]
        [InlineData("", LogTargets.File)]
        public void Le_destinazioni_si_leggono_in_piu_forme(string value, LogTargets expected)
        {
            Assert.True(LoggingSettings.TryParseTargets(value, out LogTargets targets));
            Assert.Equal(expected, targets);
        }

        [Theory]
        [InlineData("cloud")]
        [InlineData("none,file")]
        public void Destinazioni_non_valide_sono_rifiutate(string value)
        {
            Assert.False(LoggingSettings.TryParseTargets(value, out LogTargets ignored));
        }

        [Theory]
        [InlineData("warning", LogLevel.Warning)]
        [InlineData("WARN", LogLevel.Warning)]
        [InlineData("info", LogLevel.Information)]
        [InlineData("debug", LogLevel.Debug)]
        [InlineData("none", LogLevel.None)]
        public void Il_livello_si_legge_dalla_configurazione(string value, LogLevel expected)
        {
            Assert.True(LoggingSettings.TryParseLevel(value, out LogLevel level));
            Assert.Equal(expected, level);
        }

        [Fact]
        public void Le_opzioni_hanno_nomi_default_e_validazione()
        {
            var built = new ConfigurationBuilder(LoggingSettings.Options).Build();
            var settings = LoggingSettings.From(built.Configuration);

            Assert.Empty(built.Errors);
            Assert.Equal(LogTargets.File, settings.Targets);
            Assert.Equal(LogLevel.Information, settings.Level);
            Assert.Equal(30, settings.RetentionDays);
            Assert.Equal(
                new[] { "--log-targets", "--log-level", "--log-folder", "--log-retention-days" },
                LoggingSettings.Options.Select(o => o.CommandLineName));
        }

        [Fact]
        public void Valori_non_validi_sono_segnalati_con_il_nome_dell_opzione()
        {
            var built = new ConfigurationBuilder(LoggingSettings.Options)
                .AddSource(ConfigurationBuilder.CommandLineSourceName, new[]
                {
                    new KeyValuePair<string, string>("LogTargets", "cloud"),
                    new KeyValuePair<string, string>("LogLevel", "rumoroso"),
                    new KeyValuePair<string, string>("LogRetentionDays", "0")
                })
                .Build();

            Assert.Equal(3, built.Errors.Count);
            Assert.Contains(built.Errors, e => e.Contains("--log-targets"));
            Assert.Contains(built.Errors, e => e.Contains("--log-level"));
            Assert.Contains(built.Errors, e => e.Contains("--log-retention-days"));
        }

        [Fact]
        public void La_cartella_dei_log_configurata_ha_la_precedenza_su_quella_predefinita()
        {
            Assert.Equal(@"C:\Predefinita", new LoggingSettings().ResolveFolder(@"C:\Predefinita"));
            Assert.Equal(@"D:\Log", new LoggingSettings(folder: @"D:\Log").ResolveFolder(@"C:\Predefinita"));
        }

        // --- Log su file ---

        [Fact]
        public void Il_log_su_file_scrive_livello_categoria_messaggio_ed_eccezione_e_filtra_per_livello()
        {
            using (var provider = new FileLoggerProvider(_folder, LogLevel.Warning, 30))
            {
                var logger = provider.CreateLogger("Prova");
                logger.LogInformation("troppo poco importante");
                logger.LogWarning("attenzione {Valore}", 42);
                logger.LogError(new InvalidOperationException("boom"), "errore grave");
            }

            var text = ReadAllLogFiles();
            Assert.DoesNotContain("troppo poco importante", text);
            Assert.Contains("[WRN] Prova - attenzione 42", text);
            Assert.Contains("[ERR] Prova - errore grave", text);
            Assert.Contains("InvalidOperationException: boom", text);
        }

        [Fact]
        public void Alla_chiusura_la_coda_viene_svuotata()
        {
            using (var provider = new FileLoggerProvider(_folder, LogLevel.Information, 30))
            {
                var logger = provider.CreateLogger("Coda");
                for (var i = 0; i < 500; i++)
                    logger.LogInformation("messaggio {Numero}", i);
            }

            Assert.Equal(500, ReadAllLogFiles().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Length);
        }

        [Fact]
        public void I_file_piu_vecchi_della_conservazione_vengono_eliminati()
        {
            Directory.CreateDirectory(_folder);
            var old = Path.Combine(_folder, "app-20200101.log");
            var recent = Path.Combine(_folder, "app-20260101.log");
            var other = Path.Combine(_folder, "altro-20200101.log");
            File.WriteAllText(old, "vecchio");
            File.WriteAllText(recent, "recente");
            File.WriteAllText(other, "di un altro programma");
            File.SetLastWriteTime(old, DateTime.Now.AddDays(-40));
            File.SetLastWriteTime(other, DateTime.Now.AddDays(-40));

            using (var provider = new FileLoggerProvider(_folder, LogLevel.Information, 30))
                provider.CreateLogger("x").LogInformation("avvio");

            Assert.False(File.Exists(old));
            Assert.True(File.Exists(recent));
            Assert.True(File.Exists(other));
        }

        [Fact]
        public void Un_errore_di_scrittura_non_arriva_a_chi_registra()
        {
            Directory.CreateDirectory(_folder);
            var blocker = Path.Combine(_folder, "occupato");
            File.WriteAllText(blocker, "è un file, non una cartella");

            using (var provider = new FileLoggerProvider(Path.Combine(blocker, "sotto"), LogLevel.Information, 30))
            {
                var logger = provider.CreateLogger("x");
                logger.LogInformation("non si può scrivere");   // non deve sollevare nulla
            }
        }

        // --- Log su database ---

        private sealed class FailingExecutor : IDbExecutor
        {
            public Task<IReadOnlyList<T>> QueryAsync<T>(string sql, IDictionary<string, object> parameters, Func<System.Data.IDataRecord, T> map,
                CancellationToken cancellationToken = default(CancellationToken)) => throw new InvalidOperationException("database spento");

            public Task<int> ExecuteAsync(string sql, IDictionary<string, object> parameters = null,
                CancellationToken cancellationToken = default(CancellationToken)) => throw new InvalidOperationException("database spento");
        }

        [Fact]
        public async Task Il_log_su_database_scrive_le_righe_solo_dopo_Start()
        {
            using (var database = new SqliteTestDatabase())
            {
                var executor = new AdoNetExecutor(database.Connections, new SqlDialect());
                var provider = new DbLoggerProvider(executor, LogLevel.Information, new LogFileWriter(_folder, "app-fallback", 30), 30);

                var logger = provider.CreateLogger("Categoria.Prova");
                logger.LogInformation("prima dell'avvio");
                await Task.Delay(300);
                var before = await executor.QueryAsync("SELECT COUNT(*) FROM Log", null, r => r.GetInt64(0));
                Assert.Equal(0L, before.Single());   // in coda, non ancora scritto

                provider.Start();
                logger.LogError(new InvalidOperationException("boom"), "dopo l'avvio");
                provider.Dispose();

                var rows = await executor.QueryAsync(
                    "SELECT Level, Category, Message, Exception, UserName, MachineName FROM Log ORDER BY Id", null,
                    r => new[] { r.GetString(0), r.GetString(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3), r.GetString(4), r.GetString(5) });

                Assert.Equal(2, rows.Count);
                Assert.Equal(new[] { "Information", "Categoria.Prova", "prima dell'avvio" }, rows[0].Take(3));
                Assert.Null(rows[0][3]);
                Assert.Equal("Error", rows[1][0]);
                Assert.Contains("InvalidOperationException: boom", rows[1][3]);
                Assert.Equal(Environment.UserName, rows[1][4]);
                Assert.Equal(Environment.MachineName, rows[1][5]);
                Assert.False(Directory.Exists(_folder) && Directory.GetFiles(_folder).Any());   // nessun ripiego necessario
            }
        }

        [Fact]
        public void Se_il_database_non_risponde_i_messaggi_vanno_nel_file_di_ripiego()
        {
            var provider = new DbLoggerProvider(new FailingExecutor(), LogLevel.Information, new LogFileWriter(_folder, "app-fallback", 30), 30);
            provider.Start();

            provider.CreateLogger("Db").LogWarning("non scrivibile nel database");
            provider.Dispose();

            Assert.Contains("[WRN] Db - non scrivibile nel database", ReadAllLogFiles());
            Assert.True(Directory.GetFiles(_folder, "app-fallback-*.log").Any());
        }

        [Fact]
        public async Task Le_righe_piu_vecchie_della_conservazione_vengono_eliminate_all_avvio_della_scrittura()
        {
            using (var database = new SqliteTestDatabase())
            {
                var executor = new AdoNetExecutor(database.Connections, new SqlDialect());
                await executor.ExecuteAsync(DbLoggerProvider.InsertSql, new Dictionary<string, object>
                {
                    { "LoggedAt", DateTime.Now.AddDays(-90).ToString("o") }, { "Level", "Information" }, { "Category", "vecchio" },
                    { "Message", "vecchio" }, { "Exception", null }, { "UserName", "u" }, { "MachineName", "m" }
                });

                var provider = new DbLoggerProvider(executor, LogLevel.Information, new LogFileWriter(_folder, "app-fallback", 30), 30);
                provider.Start();
                provider.CreateLogger("nuovo").LogInformation("nuovo");
                provider.Dispose();

                var categories = await executor.QueryAsync("SELECT Category FROM Log", null, r => r.GetString(0));
                Assert.Equal(new[] { "nuovo" }, categories);
            }
        }

        // --- Registrazione e uso reale ---

        [Fact]
        public void AddAppLogging_scrive_su_file_e_su_database_con_ILogger()
        {
            using (var database = new SqliteTestDatabase())
            {
                var services = new ServiceCollection();
                services.AddDatabase(DatabaseProvider.Sqlite, database.Connections.ConnectionString);
                services.AddAppLogging(new LoggingSettings(LogTargets.File | LogTargets.Database, LogLevel.Information), _folder);

                var provider = services.BuildServiceProvider();
                provider.StartDeferredLogSinks();
                provider.GetRequiredService<ILogger<LoggingTests>>().LogInformation("scritto ovunque");
                provider.Dispose();

                Assert.Contains("scritto ovunque", ReadAllLogFiles());
                var rows = new AdoNetExecutor(database.Connections, new SqlDialect())
                    .QueryAsync("SELECT Message FROM Log", null, r => r.GetString(0)).GetAwaiter().GetResult();
                Assert.Equal(new[] { "scritto ovunque" }, rows);
            }
        }

        [Fact]
        public void Con_destinazione_none_o_livello_none_non_si_scrive_nulla()
        {
            foreach (var settings in new[] { new LoggingSettings(LogTargets.None), new LoggingSettings(LogTargets.File, LogLevel.None) })
            {
                var services = new ServiceCollection();
                services.AddAppLogging(settings, _folder);
                var provider = services.BuildServiceProvider();

                provider.GetRequiredService<ILogger<LoggingTests>>().LogCritical("niente");
                provider.Dispose();
            }

            Assert.Equal(string.Empty, ReadAllLogFiles());
        }

        // --- Il mediator registra ogni richiesta ---

        private sealed class CaptureProvider : ILoggerProvider, ILogger
        {
            public List<string> Lines { get; } = new List<string>();

            public ILogger CreateLogger(string categoryName) => this;
            public void Dispose() { }
            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                lock (Lines)
                    Lines.Add(logLevel + "|" + formatter(state, exception) + (exception == null ? "" : "|" + exception.GetType().Name));
            }
        }

        private sealed class Ping : IRequest<string>
        {
            public string Text { get; set; }
        }

        private sealed class PingHandler : IRequestHandler<Ping, string>
        {
            public Task<string> Handle(Ping request, CancellationToken cancellationToken)
            {
                if (request.Text == "boom")
                    throw new InvalidOperationException("boom");
                return Task.FromResult("ok");
            }
        }

        private sealed class PingValidator : IValidator<Ping>
        {
            public IEnumerable<string> Validate(Ping request)
            {
                if (string.IsNullOrEmpty(request.Text))
                    yield return "Testo mancante.";
            }
        }

        private static IMediator CreateMediator(CaptureProvider capture)
        {
            var services = new ServiceCollection();
            services.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug).AddProvider(capture));
            services.AddMediator();
            services.AddTransient<IRequestHandler<Ping, string>, PingHandler>();
            services.AddTransient<IValidator<Ping>, PingValidator>();
            return services.BuildServiceProvider().GetRequiredService<IMediator>();
        }

        [Fact]
        public async Task Il_mediator_registra_le_richieste_eseguite_non_valide_e_fallite()
        {
            var capture = new CaptureProvider();
            var mediator = CreateMediator(capture);

            await mediator.Send(new Ping { Text = "x" });
            await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(new Ping()));
            await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new Ping { Text = "boom" }));

            Assert.Contains(capture.Lines, l => l.StartsWith("Debug|Richiesta Ping eseguita in "));
            Assert.Contains("Warning|Richiesta Ping non valida: Testo mancante.", capture.Lines);
            Assert.Contains(capture.Lines, l => l.StartsWith("Error|Richiesta Ping fallita dopo ") && l.EndsWith("|InvalidOperationException"));
            Assert.Equal(3, capture.Lines.Count);
        }

        [Fact]
        public async Task Senza_logging_registrato_il_mediator_funziona_comunque()
        {
            var services = new ServiceCollection();
            services.AddMediator();
            services.AddTransient<IRequestHandler<Ping, string>, PingHandler>();

            Assert.Equal("ok", await services.BuildServiceProvider().GetRequiredService<IMediator>().Send(new Ping { Text = "x" }));
        }
    }
}
