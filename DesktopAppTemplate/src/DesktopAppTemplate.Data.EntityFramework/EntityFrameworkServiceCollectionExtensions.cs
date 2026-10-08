using DesktopAppTemplate.Features.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Data.EntityFramework
{
    public static class EntityFrameworkServiceCollectionExtensions
    {
        /// <summary>Usa Entity Framework 6 per le attività.</summary>
        public static IServiceCollection AddEntityFrameworkData(this IServiceCollection services)
        {
            services.AddSingleton<ITaskRepository, EntityFrameworkTaskRepository>();
            return services;
        }
    }
}
