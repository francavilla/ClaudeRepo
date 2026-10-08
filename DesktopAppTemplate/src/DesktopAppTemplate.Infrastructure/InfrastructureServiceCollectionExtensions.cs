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

        /// <summary>Registra le implementazioni concrete. Le impostazioni derivano dalla configurazione (<see cref="IAppConfiguration"/>, registrata dall'host).</summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IClock, SystemClock>();
            services.AddSingleton<IAppInfo, AppInfo>();
            services.AddSingleton(provider => StorageSettings.From(provider.GetRequiredService<IAppConfiguration>()));
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
