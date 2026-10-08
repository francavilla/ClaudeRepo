using System.Collections.Generic;
using System.Linq;
using DesktopAppTemplate.Core.Configuration;
using DesktopAppTemplate.Core.Hosting;
using DesktopAppTemplate.Features.Tasks;
using DesktopAppTemplate.Infrastructure;

namespace DesktopAppTemplate.Host
{
    /// <summary>
    /// Catalogo di tutte le opzioni dell'applicazione. Ogni modulo dichiara le proprie (<c>XxxSettings.Options</c>);
    /// qui si raccolgono. Per aggiungere un parametro: dichiararlo nel modulo e aggiungere una riga qui.
    /// Da questo elenco derivano App.config, riga di comando, validazione e <c>--help</c>.
    /// </summary>
    internal static class AppOptions
    {
        public static IReadOnlyList<OptionDefinition> All { get; } = UiSettings.Options
            .Concat(StorageSettings.Options)
            .Concat(TaskSettings.Options)
            .ToList();
    }
}
