using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Features.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IClock, SystemClock>();
            services.AddSingleton<IAppInfo, AppInfo>();
            services.AddSingleton<ITaskRepository>(provider =>
            {
                var repository = new InMemoryTaskRepository();
                repository.Seed(provider.GetRequiredService<IClock>().Now);
                return repository;
            });
            return services;
        }
    }
}
