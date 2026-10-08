using System;
using System.Configuration;
using System.Linq;
using DesktopAppTemplate.Core;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Configuration;
using DesktopAppTemplate.Core.Hosting;
using DesktopAppTemplate.Features;
using DesktopAppTemplate.Infrastructure;
using DesktopAppTemplate.UI.WinForms;
using DesktopAppTemplate.UI.Wpf;
using Microsoft.Extensions.DependencyInjection;

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
                try
                {
                    StorageBootstrapper.Initialize(provider);
                }
                catch (Exception ex)
                {
                    ConsoleOutput.Show(info.Name, "Impossibile preparare l'archivio dei dati: " + ex.Message, isError: true);
                    return ExitStorageError;
                }

                return provider.GetRequiredService<IUiShell>().Run();
            }
        }
    }
}
