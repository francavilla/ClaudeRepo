using DesktopAppTemplate.Features.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Data.Dapper
{
    public static class DapperServiceCollectionExtensions
    {
        /// <summary>Usa Dapper per le attività.</summary>
        public static IServiceCollection AddDapperData(this IServiceCollection services)
        {
            services.AddSingleton<ITaskRepository, DapperTaskRepository>();
            return services;
        }
    }
}
