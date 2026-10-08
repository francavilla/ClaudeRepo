using System;
using System.IO;
using System.Text;
using PasswordGen.Core.History;

namespace PasswordGen.Core.Sync
{
    /// <summary>
    /// Conserva la frase segreta della sincronizzazione in un file cifrato con la protezione del dispositivo
    /// (DPAPI su Windows, chiave del Keystore su Android), così non va reinserita a ogni sincronizzazione.
    /// </summary>
    public sealed class SyncPassphraseStore
    {
        private readonly string _path;
        private readonly ISecretProtector _protector;

        public SyncPassphraseStore(string path, ISecretProtector protector)
        {
            _path = path;
            _protector = protector;
        }

        /// <summary>Null se manca o non si decifra.</summary>
        public string Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    return Encoding.UTF8.GetString(_protector.Unprotect(File.ReadAllBytes(_path)));
                }
            }
            catch (Exception)
            {
                // File illeggibile (chiave cambiata): come se non ci fosse una frase salvata.
            }

            return null;
        }

        public void Save(string passphrase)
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temp = _path + ".tmp";
            File.WriteAllBytes(temp, _protector.Protect(Encoding.UTF8.GetBytes(passphrase)));
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            File.Move(temp, _path);
        }

        public void Delete()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
    }
}
