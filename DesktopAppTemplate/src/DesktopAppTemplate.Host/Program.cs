using System;
using System.Configuration;
using DesktopAppTemplate.Core;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Hosting;
using DesktopAppTemplate.Features;
using DesktopAppTemplate.Infrastructure;
using DesktopAppTemplate.UI.WinForms;
using DesktopAppTemplate.UI.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Host
{
    internal static class Program
    {
        /// <summary>
        /// Avvio: <c>DesktopAppTemplate.exe --ui wpf</c> oppure <c>--ui winforms</c>.
        /// Senza argomento vale l'impostazione "Ui" in App.config (predefinita: wpf).
        /// </summary>
        [STAThread]
        private static int Main(string[] args)
        {
            var ui = UiSelector.Resolve(args, ConfigurationManager.AppSettings["Ui"]);

            var services = new ServiceCollection();
            services.AddCore()
                    .AddFeatures()
                    .AddInfrastructure();

            // L'unica differenza tra le due versioni è questa riga: la logica è identica.
            if (ui == UiKind.WinForms)
                services.AddWinFormsUi();
            else
                services.AddWpfUi();

            using (var provider = services.BuildServiceProvider())
            {
                return provider.GetRequiredService<IUiShell>().Run();
            }
        }
    }
}
