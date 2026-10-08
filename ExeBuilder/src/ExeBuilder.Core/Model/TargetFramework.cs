using System;
using System.Globalization;

namespace ExeBuilder.Core.Model
{
    public enum FrameworkFamily
    {
        Unknown,

        /// <summary>.NET Framework 1.x - 4.8.x</summary>
        NetFramework,

        /// <summary>.NET Core 1.0 - 3.1 e .NET 5+.</summary>
        NetCore,

        /// <summary>.NET Standard: solo librerie, non produce eseguibili.</summary>
        NetStandard
    }

    /// <summary>
    /// Target framework di un progetto, ricavato dal moniker (net48, net8.0-windows, ...)
    /// o dalla coppia TargetFrameworkIdentifier/TargetFrameworkVersion dei progetti classici.
    /// </summary>
    public sealed class TargetFramework : IEquatable<TargetFramework>
    {
        private TargetFramework(string moniker, FrameworkFamily family, Version version, string platform)
        {
            Moniker = moniker;
            Family = family;
            Version = version;
            Platform = platform;
        }

        /// <summary>Moniker così come va passato a <c>dotnet -f</c> (es. "net8.0-windows").</summary>
        public string Moniker { get; private set; }

        public FrameworkFamily Family { get; private set; }

        public Version Version { get; private set; }

        /// <summary>Parte dopo il trattino (es. "windows", "windows10.0.19041.0"), oppure null.</summary>
        public string Platform { get; private set; }

        public bool CanProduceExecutable
        {
            get { return Family == FrameworkFamily.NetFramework || Family == FrameworkFamily.NetCore; }
        }

        public string DisplayName
        {
            get
            {
                switch (Family)
                {
                    case FrameworkFamily.NetFramework:
                        return ".NET Framework " + FormatVersion(Version);
                    case FrameworkFamily.NetCore:
                        var name = (Version.Major < 5 ? ".NET Core " : ".NET ") + Version.Major + "." + Version.Minor;
                        if (!string.IsNullOrEmpty(Platform))
                        {
                            var os = Platform.StartsWith("windows", StringComparison.OrdinalIgnoreCase) ? "Windows" : Platform;
                            name += " (" + os + ")";
                        }

                        return name;
                    case FrameworkFamily.NetStandard:
                        return ".NET Standard " + Version.Major + "." + Version.Minor;
                    default:
                        return Moniker + " (non riconosciuto)";
                }
            }
        }

        /// <summary>
        /// Interpreta un target framework moniker SDK-style: net48, net472, net8.0, net8.0-windows,
        /// netcoreapp3.1, netstandard2.0. Accetta anche la forma estesa ".NETFramework,Version=v4.8".
        /// </summary>
        public static TargetFramework Parse(string moniker)
        {
            if (string.IsNullOrWhiteSpace(moniker))
            {
                throw new ArgumentException("Moniker vuoto.", "moniker");
            }

            var original = moniker.Trim();
            var text = original.ToLowerInvariant();

            var comma = text.IndexOf(",version=v", StringComparison.Ordinal);
            if (comma > 0)
            {
                return FromIdentifier(original.Substring(0, comma), original.Substring(comma + ",version=".Length));
            }

            string platform = null;
            var dash = text.IndexOf('-');
            if (dash > 0)
            {
                platform = original.Substring(dash + 1);
                text = text.Substring(0, dash);
            }

            Version version;
            if (text.StartsWith("netcoreapp", StringComparison.Ordinal) && TryParseDotted(text.Substring(10), out version))
            {
                return new TargetFramework(original, FrameworkFamily.NetCore, version, platform);
            }

            if (text.StartsWith("netstandard", StringComparison.Ordinal) && TryParseDotted(text.Substring(11), out version))
            {
                return new TargetFramework(original, FrameworkFamily.NetStandard, version, platform);
            }

            if (text.StartsWith("net", StringComparison.Ordinal) && text.Length > 3)
            {
                var rest = text.Substring(3);
                if (rest.IndexOf('.') >= 0 ? TryParseDotted(rest, out version) : TryParseCompact(rest, out version))
                {
                    var family = version.Major >= 5 ? FrameworkFamily.NetCore : FrameworkFamily.NetFramework;
                    return new TargetFramework(original, family, version, platform);
                }
            }

            return new TargetFramework(original, FrameworkFamily.Unknown, new Version(0, 0), platform);
        }

        /// <summary>
        /// Progetti classici: TargetFrameworkIdentifier (default ".NETFramework") + TargetFrameworkVersion ("v4.8").
        /// </summary>
        public static TargetFramework FromIdentifier(string identifier, string versionText)
        {
            Version version;
            var cleaned = (versionText ?? string.Empty).Trim().TrimStart('v', 'V');
            if (!TryParseDotted(cleaned, out version))
            {
                version = new Version(0, 0);
            }

            identifier = string.IsNullOrWhiteSpace(identifier) ? ".NETFramework" : identifier.Trim();
            FrameworkFamily family;
            string moniker;
            switch (identifier.ToLowerInvariant())
            {
                case ".netframework":
                    family = FrameworkFamily.NetFramework;
                    moniker = "net" + version.Major + Math.Max(version.Minor, 0) + (version.Build > 0 ? version.Build.ToString(CultureInfo.InvariantCulture) : string.Empty);
                    break;
                case ".netcoreapp":
                    family = FrameworkFamily.NetCore;
                    moniker = (version.Major >= 5 ? "net" : "netcoreapp") + version.Major + "." + version.Minor;
                    break;
                case ".netstandard":
                    family = FrameworkFamily.NetStandard;
                    moniker = "netstandard" + version.Major + "." + version.Minor;
                    break;
                default:
                    family = FrameworkFamily.Unknown;
                    moniker = identifier + ",Version=v" + cleaned;
                    break;
            }

            return new TargetFramework(moniker, family, version, null);
        }

        /// <summary>Versione nel formato delle cartelle "Reference Assemblies" (es. 4.8, 4.7.2).</summary>
        public static string FormatVersion(Version version)
        {
            return version.Build > 0
                ? version.ToString(3)
                : version.ToString(2);
        }

        public bool Equals(TargetFramework other)
        {
            return other != null && string.Equals(Moniker, other.Moniker, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TargetFramework);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Moniker);
        }

        public override string ToString()
        {
            return Moniker;
        }

        private static bool TryParseDotted(string text, out Version version)
        {
            version = null;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (text.IndexOf('.') < 0)
            {
                int major;
                if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out major))
                {
                    return false;
                }

                version = new Version(major, 0);
                return true;
            }

            return Version.TryParse(text, out version);
        }

        // "48" -> 4.8, "472" -> 4.7.2, "5" -> 5.0
        private static bool TryParseCompact(string digits, out Version version)
        {
            version = null;
            if (digits.Length == 0 || digits.Length > 3)
            {
                return false;
            }

            foreach (var c in digits)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            var major = digits[0] - '0';
            var minor = digits.Length > 1 ? digits[1] - '0' : 0;
            version = digits.Length > 2 ? new Version(major, minor, digits[2] - '0') : new Version(major, minor);
            return true;
        }
    }
}
