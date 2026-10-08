using System;
using System.IO;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Features.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public const string TasksFileName = "tasks.json";

        /// <param name="dataFolder">
        /// Cartella in cui salvare i dati (le variabili d'ambiente, es. %USERPROFILE%, vengono espanse).
        /// Se vuota: <c>%LocalAppData%\&lt;nome app&gt;</c>, che non richiede permessi di amministratore.
        /// </param>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string dataFolder = null)
        {
            services.AddSingleton<IClock, SystemClock>();
            services.AddSingleton<IAppInfo, AppInfo>();
            services.AddSingleton<ITaskRepository>(provider =>
            {
                var folder = ResolveDataFolder(dataFolder, provider.GetRequiredService<IAppInfo>().Name);
                var clock = provider.GetRequiredService<IClock>();

                // Al primo avvio (file assente) si parte con le attività di esempio.
                return new JsonFileTaskRepository(Path.Combine(folder, TasksFileName), () => SampleTasks.Create(clock.Now));
            });
            return services;
        }

        public static string ResolveDataFolder(string configuredFolder, string appName)
        {
            if (!string.IsNullOrWhiteSpace(configuredFolder))
                return Environment.ExpandEnvironmentVariables(configuredFolder.Trim());

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName);
        }
    }
}
