using DesktopAppTemplate.Core.Data;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Data.Dal
{
    public static class DalServiceCollectionExtensions
    {
        /// <summary>
        /// Usa DAL per l'accesso ai dati: registra <see cref="DalExecutor"/> al posto dell'executor predefinito
        /// (da chiamare DOPO <c>AddDatabase</c>: vale l'ultima registrazione) e il gestore di transazioni esplicite.
        /// </summary>
        public static IServiceCollection AddDalAdapter(this IServiceCollection services)
        {
            return services.AddDalAdapter<DalGateway>();
        }

        /// <summary>Come <see cref="AddDalAdapter(IServiceCollection)"/> con un'altra implementazione di <see cref="IDalGateway"/> (es. nei test).</summary>
        public static IServiceCollection AddDalAdapter<TGateway>(this IServiceCollection services) where TGateway : class, IDalGateway
        {
            services.AddSingleton<IDalGateway, TGateway>();
            services.AddSingleton<DalLock>();
            services.AddSingleton<IDbExecutor, DalExecutor>();
            services.AddSingleton<ITransactionRunner, DalTransactionRunner>();
            return services;
        }
    }
}
