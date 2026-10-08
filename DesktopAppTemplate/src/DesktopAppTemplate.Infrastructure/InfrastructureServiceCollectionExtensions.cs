using System.IO;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Configuration;
using DesktopAppTemplate.Features.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public const string TasksFileName = "tasks.json";

        /// <summary>
        /// Registra le implementazioni concrete di uso generale (orologio, informazioni sull'app, impostazioni di archiviazione).
        /// L'archivio dei dati si sceglie a parte (<see cref="AddFileStorage"/> o i progetti Data.*): vedi <c>Host/StorageRegistration</c>.
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IClock, SystemClock>();
            services.AddSingleton<IAppInfo, AppInfo>();
            services.AddSingleton(provider => StorageSettings.From(provider.GetRequiredService<IAppConfiguration>()));
            return services;
        }

        /// <summary>Salva le attività in un file JSON nella cartella dei dati (nessun database).</summary>
        public static IServiceCollection AddFileStorage(this IServiceCollection services)
        {
            services.AddSingleton<ITaskRepository>(provider =>
            {
                var folder = provider.GetRequiredService<StorageSettings>().ResolveFolder(provider.GetRequiredService<IAppInfo>().Name);
                var clock = provider.GetRequiredService<IClock>();

                // Al primo avvio (file assente) si parte con le attività di esempio.
                return new JsonFileTaskRepository(Path.Combine(folder, TasksFileName), () => SampleTasks.Create(clock.Now));
            });
            return services;
        }
    }
}
