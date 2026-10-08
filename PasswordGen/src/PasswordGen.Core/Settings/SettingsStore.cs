using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace PasswordGen.Core.Settings
{
    /// <summary>Lettura e scrittura delle preferenze in un file JSON. Se il file manca o è danneggiato si usano i valori predefiniti.</summary>
    public sealed class SettingsStore
    {
        private readonly string _path;

        public SettingsStore(string path)
        {
            _path = path;
        }

        /// <summary>%AppData%\PasswordGen\settings.json</summary>
        public static string DefaultPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PasswordGen",
                    "settings.json");
            }
        }

        public AppSettings Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    using (var stream = File.OpenRead(_path))
                    {
                        var settings = (AppSettings)new DataContractJsonSerializer(typeof(AppSettings)).ReadObject(stream);
                        return settings.Normalized();
                    }
                }
            }
            catch (Exception)
            {
                // File illeggibile o non valido (I/O, permessi, JSON malformato): si riparte dai valori predefiniti.
            }

            return new AppSettings();
        }

        public void Save(AppSettings settings)
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Scrittura su file temporaneo e poi sostituzione: un crash non lascia un file a metà.
            var temp = _path + ".tmp";
            using (var stream = File.Create(temp))
            {
                new DataContractJsonSerializer(typeof(AppSettings)).WriteObject(stream, settings);
            }

            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            File.Move(temp, _path);
        }
    }
}
