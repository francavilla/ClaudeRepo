using System.IO;

namespace PasswordGen.Core.Sync
{
    /// <summary>Dove sta il file di sincronizzazione: un file di una cartella (Windows) o un documento scelto dall'utente (Android).</summary>
    public interface ISyncStorage
    {
        /// <summary>Contenuto del file, o null se non esiste ancora.</summary>
        byte[] Read();

        void Write(byte[] data);
    }

    /// <summary>
    /// Controllo leggero e rapido: restituisce una «impronta» della versione corrente del file (null se non esiste o non si può sapere).
    /// Se l'impronta non cambia tra due controlli, il file non è cambiato e non serve leggerlo né sincronizzare.
    /// </summary>
    public interface IChangeProbe
    {
        string Probe();
    }

    /// <summary>File normale, ad esempio in una cartella sincronizzata da Google Drive.</summary>
    public sealed class FileSyncStorage : ISyncStorage, IChangeProbe
    {
        private readonly string _path;

        public FileSyncStorage(string path)
        {
            _path = path;
        }

        public byte[] Read()
        {
            return File.Exists(_path) ? File.ReadAllBytes(_path) : null;
        }

        public string Probe()
        {
            var info = new FileInfo(_path);
            return info.Exists ? info.Length + ":" + info.LastWriteTimeUtc.Ticks : null;
        }

        public void Write(byte[] data)
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // File temporaneo e poi sostituzione: un client di sincronizzazione non vede mai un file a metà.
            var temp = _path + ".tmp";
            File.WriteAllBytes(temp, data);
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            File.Move(temp, _path);
        }
    }
}
