using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace PasswordGen.Core.History
{
    /// <summary>Lettura e scrittura dello storico in un file cifrato (history.dat).</summary>
    public sealed class HistoryStore
    {
        private readonly string _path;
        private readonly ISecretProtector _protector;

        public HistoryStore(string path, ISecretProtector protector)
        {
            _path = path;
            _protector = protector;
        }

        /// <summary>%AppData%\PasswordGen\history.dat</summary>
        public static string DefaultPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PasswordGen",
                    "history.dat");
            }
        }

        public bool Exists
        {
            get { return File.Exists(_path); }
        }

        /// <summary>Se il file manca, non si decifra o è danneggiato (altro utente, altro computer) lo storico riparte vuoto.</summary>
        public PasswordHistory Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var plain = _protector.Unprotect(File.ReadAllBytes(_path));
                    using (var stream = new MemoryStream(plain))
                    {
                        return (PasswordHistory)new DataContractJsonSerializer(typeof(PasswordHistory)).ReadObject(stream);
                    }
                }
            }
            catch (Exception)
            {
                // Dati illeggibili: si riparte da uno storico vuoto.
            }

            return new PasswordHistory();
        }

        public void Save(PasswordHistory history)
        {
            byte[] plain;
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(PasswordHistory)).WriteObject(stream, history);
                plain = stream.ToArray();
            }

            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Scrittura su file temporaneo e poi sostituzione: un crash non lascia un file a metà.
            var temp = _path + ".tmp";
            File.WriteAllBytes(temp, _protector.Protect(plain));
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            File.Move(temp, _path);
        }

        /// <summary>Elimina il file dello storico.</summary>
        public void Delete()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
    }
}
