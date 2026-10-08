using DesktopAppTemplate.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.UI.WinForms
{
    public static class WinFormsServiceCollectionExtensions
    {
        /// <summary>Registra l'interfaccia Windows Forms.</summary>
        public static IServiceCollection AddWinFormsUi(this IServiceCollection services)
        {
            services.AddSingleton<IUiShell, WinFormsShell>();
            services.AddSingleton<IDialogService, WinFormsDialogService>();
            services.AddSingleton<IUiDescriptor, WinFormsUiDescriptor>();
            return services;
        }
    }
}
