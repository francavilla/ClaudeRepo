using System;
using System.IO;
using System.Runtime.Serialization.Json;
using PasswordGen.Core.History;

namespace PasswordGen.Core.Security
{
    /// <summary>
    /// File cifrato (lo stesso cifrario dello storico) con l'hash del PIN o della password e il conteggio dei tentativi sbagliati:
    /// senza la chiave del telefono non si possono azzerare i tentativi né sostituire l'hash.
    /// </summary>
    public sealed class LockCredentialStore
    {
        private readonly string _path;
        private readonly ISecretProtector _protector;

        public LockCredentialStore(string path, ISecretProtector protector)
        {
            _path = path;
            _protector = protector;
        }

        /// <summary>Restituisce null se il file manca o non si decifra.</summary>
        public LockCredential Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var plain = _protector.Unprotect(File.ReadAllBytes(_path));
                    using (var stream = new MemoryStream(plain))
                    {
                        return (LockCredential)new DataContractJsonSerializer(typeof(LockCredential)).ReadObject(stream);
                    }
                }
            }
            catch (Exception)
            {
                // File illeggibile (chiave cambiata o file alterato): come se non ci fosse alcun PIN.
            }

            return null;
        }

        public void Save(LockCredential credential)
        {
            byte[] plain;
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(LockCredential)).WriteObject(stream, credential);
                plain = stream.ToArray();
            }

            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temp = _path + ".tmp";
            File.WriteAllBytes(temp, _protector.Protect(plain));
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
