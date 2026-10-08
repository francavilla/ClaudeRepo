using DesktopAppTemplate.Features.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Data.Ado
{
    public static class AdoNetServiceCollectionExtensions
    {
        /// <summary>Usa ADO.NET (tramite <c>IDbExecutor</c>) per le attività.</summary>
        public static IServiceCollection AddAdoNetData(this IServiceCollection services)
        {
            services.AddSingleton<ITaskRepository, AdoNetTaskRepository>();
            return services;
        }
    }
}
