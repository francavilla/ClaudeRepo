using DesktopAppTemplate.Core.Data;
using DesktopAppTemplate.Data;
using DesktopAppTemplate.Data.Ado;
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

            // >>> AGGANCIO LIBRERIA ESISTENTE (esempio, da attivare quando c'è l'adattatore):
            // services.AddSingleton<IDbExecutor, MiaLibreriaExecutor>();

            switch (storage.DataAccess)
            {
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
