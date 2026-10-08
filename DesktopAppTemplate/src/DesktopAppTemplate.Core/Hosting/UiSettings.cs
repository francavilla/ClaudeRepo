using System.Collections.Generic;
using DesktopAppTemplate.Core.Configuration;

namespace DesktopAppTemplate.Core.Hosting
{
    /// <summary>Impostazioni dell'interfaccia utente: quale UI avviare.</summary>
    public sealed class UiSettings
    {
        public const string UiKey = "Ui";
        public const UiKind Default = UiKind.Wpf;

        public static readonly OptionDefinition UiOption = new OptionDefinition(
            UiKey, "Interfaccia da avviare.", "wpf|winforms", "wpf",
            validate: value => TryParse(value, out UiKind ignored) ? null : "valori ammessi: wpf, winforms");

        public UiSettings(UiKind ui)
        {
            Ui = ui;
        }

        public UiKind Ui { get; }

        /// <summary>Opzioni da dichiarare nel catalogo dell'applicazione.</summary>
        public static IEnumerable<OptionDefinition> Options => new[] { UiOption };

        public static UiSettings From(IAppConfiguration configuration)
        {
            UiKind kind;
            return new UiSettings(TryParse(configuration.GetString(UiKey), out kind) ? kind : Default);
        }

        public static bool TryParse(string value, out UiKind kind)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "wpf":
                    kind = UiKind.Wpf;
                    return true;
                case "winforms":
                case "windowsforms":
                case "wf":
                    kind = UiKind.WinForms;
                    return true;
                default:
                    kind = Default;
                    return false;
            }
        }
    }
}
