using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace SolutionDoctor.Presentation.Services
{
    /// <summary>
    /// Preferenze in un semplice file di testo "chiave=valore". Lettura e scrittura sono "best effort":
    /// un file mancante, illeggibile o non scrivibile non deve mai impedire l'uso dell'applicazione.
    /// </summary>
    public sealed class FileSettingsStore : ISettingsStore
    {
        private readonly string _path;

        public FileSettingsStore(string path)
        {
            _path = path;
        }

        public AppSettings Load()
        {
            var settings = new AppSettings();
            try
            {
                if (!File.Exists(_path))
                {
                    return settings;
                }

                foreach (var line in File.ReadAllLines(_path, Encoding.UTF8))
                {
                    var separator = line.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }

                    var key = line.Substring(0, separator).Trim();
                    var value = line.Substring(separator + 1).Trim();
                    switch (key)
                    {
                        case "LastPath":
                            settings.LastPath = value;
                            break;
                        case "UseGit":
                            bool useGit;
                            if (bool.TryParse(value, out useGit))
                            {
                                settings.UseGit = useGit;
                            }

                            break;
                        case "GitMonths":
                            int months;
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out months) && months > 0)
                            {
                                settings.GitMonths = months;
                            }

                            break;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // si usano i valori predefiniti
            }

            return settings;
        }

        public void Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                File.WriteAllLines(
                    _path,
                    new[]
                    {
                        "LastPath=" + settings.LastPath,
                        "UseGit=" + settings.UseGit,
                        "GitMonths=" + settings.GitMonths.ToString(CultureInfo.InvariantCulture)
                    },
                    new UTF8Encoding(false));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // le preferenze non sono essenziali
            }
        }
    }
}
