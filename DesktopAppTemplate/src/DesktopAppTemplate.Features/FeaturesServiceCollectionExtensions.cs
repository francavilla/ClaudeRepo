using DesktopAppTemplate.Core;
using DesktopAppTemplate.Core.Configuration;
using DesktopAppTemplate.Core.Mvvm;
using DesktopAppTemplate.Features.About;
using DesktopAppTemplate.Features.Shell;
using DesktopAppTemplate.Features.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Features
{
    public static class FeaturesServiceCollectionExtensions
    {
        /// <summary>Registra handler, validatori e view model di tutte le slice.</summary>
        public static IServiceCollection AddFeatures(this IServiceCollection services)
        {
            services.AddHandlersFromAssembly(typeof(FeaturesServiceCollectionExtensions).Assembly);

            // Le impostazioni della slice derivano dalla configurazione (IAppConfiguration, registrata dall'host).
            services.AddSingleton(provider => TaskSettings.From(provider.GetRequiredService<IAppConfiguration>()));

            // Una riga per pagina: il menu laterale si costruisce da solo.
            services.AddSingleton<PageViewModel, TaskListViewModel>();
            services.AddSingleton<PageViewModel, AboutViewModel>();

            services.AddSingleton<MainViewModel>();
            return services;
        }
    }
}
