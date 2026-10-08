using DesktopAppTemplate.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.UI.Wpf
{
    public static class WpfServiceCollectionExtensions
    {
        /// <summary>Registra l'interfaccia WPF.</summary>
        public static IServiceCollection AddWpfUi(this IServiceCollection services)
        {
            services.AddSingleton<IUiShell, WpfShell>();
            services.AddSingleton<IDialogService, WpfDialogService>();
            services.AddSingleton<IUiDescriptor, WpfUiDescriptor>();
            return services;
        }
    }
}
