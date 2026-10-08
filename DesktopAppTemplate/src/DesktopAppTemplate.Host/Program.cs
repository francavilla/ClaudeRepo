using System;
using System.Configuration;
using System.Linq;
using DesktopAppTemplate.Core;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Configuration;
using DesktopAppTemplate.Core.Hosting;
using DesktopAppTemplate.Features;
using DesktopAppTemplate.Infrastructure;
using DesktopAppTemplate.Logging;
using DesktopAppTemplate.UI.WinForms;
using DesktopAppTemplate.UI.Wpf;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DesktopAppTemplate.Host
{
    internal static class Program
    {
        private const int ExitInvalidArguments = 2;
        private const int ExitStorageError = 3;

        /// <summary>
        /// Avvio. La configurazione si ottiene sovrapponendo, dal più debole al più forte:
        /// valori predefiniti, App.config, riga di comando (vedi <see cref="AppOptions"/>).
        /// <c>--help</c> e <c>--version</c> mostrano le informazioni ed escono; argomenti o valori non validi
        /// vengono segnalati (codice di uscita 2) senza aprire l'interfaccia; un archivio non raggiungibile
        /// (database, migrazioni) viene segnalato con codice di uscita 3.
        /// </summary>
        [STAThread]
        private static int Main(string[] args)
        {
            var info = new AppInfo();
            var options = AppOptions.All;

            var commandLine = CommandLineParser.Parse(args, options);
            if (commandLine.HelpRequested)
            {
                ConsoleOutput.Show(info.Name, UsageText.Build(info.Name, info.Version, options));
                return 0;
            }

            if (commandLine.VersionRequested)
            {
                ConsoleOutput.Show(info.Name, info.Name + " " + info.Version);
                return 0;
            }

            var built = new ConfigurationBuilder(options)
                .AddNameValueSource("App.config", ConfigurationManager.AppSettings)
                .AddSource(ConfigurationBuilder.CommandLineSourceName, commandLine.Values)
                .Build();

            var storage = StorageSettings.From(built.Configuration);
            var logging = LoggingSettings.From(built.Configuration);
            var errors = commandLine.Errors.Concat(built.Errors).Concat(storage.Validate()).ToList();
            if (errors.Count > 0)
            {
                ConsoleOutput.Show(info.Name, string.Join(Environment.NewLine, errors)
                                              + Environment.NewLine + Environment.NewLine
                                              + "Usa --help per l'elenco delle opzioni.", isError: true);
                return ExitInvalidArguments;
            }

            var configuration = built.Configuration;

            var services = new ServiceCollection();
            services.AddMediator()
                    .AddAppContext(configuration)
                    .AddAppLogging(logging, logging.ResolveFolder(Path.Combine(storage.ResolveFolder(info.Name), "logs")))
                    .AddFeatures()
                    .AddInfrastructure()
                    .AddStorage(storage, info.Name);

            // L'unica differenza tra le due versioni è questa scelta: la logica è identica.
            if (UiSettings.From(configuration).Ui == UiKind.WinForms)
                services.AddWinFormsUi();
            else
                services.AddWpfUi();

            using (var provider = services.BuildServiceProvider())
            {
                var log = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DesktopAppTemplate");
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                    log.LogCritical(e.ExceptionObject as Exception, "Eccezione non gestita (terminazione: {IsTerminating})", e.IsTerminating);

                log.LogInformation("Avvio {App} v{Version}: interfaccia {Ui}, database {Storage} con accesso {DataAccess}, log {LogTargets} ({LogLevel})",
                    info.Name, info.Version, UiSettings.From(configuration).Ui, storage.Storage, storage.DataAccess, logging.Targets, logging.Level);

                try
                {
                    var migration = StorageBootstrapper.Initialize(provider);
                    if (migration != null)
                        log.LogInformation("Schema del database aggiornato: versione precedente {Previous}, script applicati {Applied}",
                            migration.PreviousVersion, migration.AppliedCount);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Impossibile preparare l'archivio dei dati");
                    provider.StartDeferredLogSinks();   // il log su database ripiega sul file
                    ConsoleOutput.Show(info.Name, "Impossibile preparare l'archivio dei dati: " + ex.Message, isError: true);
                    return ExitStorageError;
                }

                // Lo schema è pronto: il log su database può iniziare a scrivere (finora i messaggi erano in coda).
                provider.StartDeferredLogSinks();

                var exitCode = provider.GetRequiredService<IUiShell>().Run();
                log.LogInformation("Arresto (codice di uscita {ExitCode})", exitCode);
                return exitCode;
            }
        }
    }
}
