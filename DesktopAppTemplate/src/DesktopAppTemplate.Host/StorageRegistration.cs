using DesktopAppTemplate.Core.Data;
using DesktopAppTemplate.Data;
using DesktopAppTemplate.Data.Dal;
using DesktopAppTemplate.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Host
{
    /// <summary>
    /// Registra l'accesso ai dati: database (SQLite o SQL Server) e repository ADO.NET. È anche il punto in cui agganciare
    /// una libreria esistente: dopo <c>AddDatabase</c> si registra la propria implementazione di <see cref="IDbExecutor"/> /
    /// <see cref="IDbConnectionFactory"/> / <see cref="ISqlDialect"/> (vale l'ultima registrazione); per DAL esiste già
    /// l'adattatore <c>Data.Dal</c>, che si attiva con <c>--data-access dal</c>.
    /// </summary>
    internal static class StorageRegistration
    {
        public static IServiceCollection AddStorage(this IServiceCollection services, StorageSettings storage, string appName)
        {
            services.AddDatabase(storage.Provider, storage.ResolveConnectionString(appName));

            // Con DAL l'adattatore sostituisce l'executor predefinito (va registrato dopo AddDatabase);
            // il collegamento a DAL è in Data.Dal/DalGateway.cs.
            if (storage.DataAccess == DataAccessKind.Dal)
                services.AddDalAdapter();

            return services.AddAdoNetData();
        }
    }
}
