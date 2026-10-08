using System;

namespace DesktopAppTemplate.Core.Hosting
{
    /// <summary>
    /// Sceglie l'interfaccia da avviare. Precedenza: argomento <c>--ui wpf|winforms</c> (o <c>--ui=wpf</c>),
    /// poi impostazione di configurazione, poi <see cref="Default"/>.
    /// </summary>
    public static class UiSelector
    {
        public const UiKind Default = UiKind.Wpf;

        public static UiKind Resolve(string[] args, string configuredValue)
        {
            UiKind kind;

            if (args != null)
            {
                for (var i = 0; i < args.Length; i++)
                {
                    var arg = args[i] ?? string.Empty;
                    string value = null;

                    if (arg.Equals("--ui", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                        value = args[i + 1];
                    else if (arg.StartsWith("--ui=", StringComparison.OrdinalIgnoreCase))
                        value = arg.Substring("--ui=".Length);

                    if (value != null && TryParse(value, out kind))
                        return kind;
                }
            }

            return TryParse(configuredValue, out kind) ? kind : Default;
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
