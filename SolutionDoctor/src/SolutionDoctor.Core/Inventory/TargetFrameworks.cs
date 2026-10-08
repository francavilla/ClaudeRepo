using System.Text.RegularExpressions;

namespace SolutionDoctor.Core.Inventory
{
    internal static class TargetFrameworks
    {
        private static readonly Regex FrameworkMoniker = new Regex(@"^net(?<d>\d{2,3})$", RegexOptions.IgnoreCase);

        /// <summary>Converte "v4.7.2" nel moniker "net472".</summary>
        public static string FromVersion(string version)
        {
            return "net" + version.Trim().TrimStart('v', 'V').Replace(".", string.Empty);
        }

        /// <summary>True per i moniker .NET Framework precedenti alla 4.6.2 (net35, net40, net45, net451, net452, net46, net461).</summary>
        public static bool IsOutOfSupport(string moniker)
        {
            var match = FrameworkMoniker.Match(moniker ?? string.Empty);
            if (!match.Success)
            {
                return false;
            }

            // "40" -> 400, "45" -> 450, "451" -> 451: confrontabili con 462.
            return int.Parse(match.Groups["d"].Value.PadRight(3, '0')) < 462;
        }
    }
}
