using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        /// <summary>
        /// Registra le implementazioni concrete di uso generale (orologio, informazioni sull'app, impostazioni di archiviazione).
        /// L'archivio dei dati si registra a parte, con il progetto <c>Data</c>: vedi <c>Host/StorageRegistration</c>.
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IClock, SystemClock>();
            services.AddSingleton<IAppInfo, AppInfo>();
            services.AddSingleton(provider => StorageSettings.From(provider.GetRequiredService<IAppConfiguration>()));
            return services;
        }
    }
}
