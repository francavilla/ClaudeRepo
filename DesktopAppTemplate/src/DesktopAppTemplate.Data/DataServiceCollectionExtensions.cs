using DesktopAppTemplate.Core.Data;
using DesktopAppTemplate.Data.Tasks;
using DesktopAppTemplate.Features.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Data
{
    public static class DataServiceCollectionExtensions
    {
        /// <summary>
        /// Registra le parti comuni dell'accesso al database. Sono tutte valori predefiniti sostituibili: per agganciare
        /// una libreria esistente, registrare un'implementazione propria di <see cref="IDbConnectionFactory"/>,
        /// <see cref="IDbExecutor"/> o <see cref="ISqlDialect"/> DOPO questa chiamata (vale l'ultima registrazione).
        /// </summary>
        public static IServiceCollection AddDatabase(this IServiceCollection services, DatabaseProvider provider, string connectionString)
        {
            services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(provider, connectionString));
            services.AddSingleton<ISqlDialect>(new SqlDialect());
            services.AddSingleton(new AdoNetOptions());
            services.AddSingleton<IDbExecutor, AdoNetExecutor>();
            services.AddSingleton<IDatabaseMigrator, DatabaseMigrator>();
            return services;
        }

        /// <summary>Usa ADO.NET (tramite <see cref="IDbExecutor"/>) per le attività.</summary>
        public static IServiceCollection AddAdoNetData(this IServiceCollection services)
        {
            services.AddSingleton<ITaskRepository, AdoNetTaskRepository>();
            return services;
        }
    }
}
