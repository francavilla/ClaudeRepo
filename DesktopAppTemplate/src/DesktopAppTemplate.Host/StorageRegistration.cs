using DesktopAppTemplate.Core.Data;
using DesktopAppTemplate.Data;
using DesktopAppTemplate.Data.Ado;
using DesktopAppTemplate.Data.Dal;
using DesktopAppTemplate.Data.Dapper;
using DesktopAppTemplate.Data.EntityFramework;
using DesktopAppTemplate.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Host
{
    /// <summary>
    /// Sceglie l'archivio dei dati in base alle impostazioni: file JSON oppure database con la tecnologia indicata.
    /// È anche il punto in cui agganciare una libreria esistente: dopo <c>AddDatabase</c> si registra la propria
    /// implementazione di <see cref="IDbExecutor"/> / <see cref="IDbConnectionFactory"/> / <see cref="ISqlDialect"/>
    /// (vale l'ultima registrazione) oppure un proprio repository.
    /// </summary>
    internal static class StorageRegistration
    {
        public static IServiceCollection AddStorage(this IServiceCollection services, StorageSettings storage, string appName)
        {
            if (storage.Storage == StorageKind.File)
                return services.AddFileStorage();

            services.AddDatabase(storage.Provider, storage.ResolveConnectionString(appName));

            switch (storage.DataAccess)
            {
                case DataAccessKind.Dal:
                    // Repository ADO.NET sopra la libreria DAL: l'adattatore sostituisce l'executor predefinito.
                    // Va registrato dopo AddDatabase (vale l'ultima registrazione); il collegamento a DAL è in Data.Dal/DalGateway.cs.
                    services.AddDalAdapter();
                    return services.AddAdoNetData();
                case DataAccessKind.Dapper:
                    return services.AddDapperData();
                case DataAccessKind.EntityFramework:
                    return services.AddEntityFrameworkData();
                default:
                    return services.AddAdoNetData();
            }
        }
    }
}
