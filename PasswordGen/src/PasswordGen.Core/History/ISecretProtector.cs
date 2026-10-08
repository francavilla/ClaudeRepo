using System;
using System.Security.Cryptography;
using System.Text;

namespace PasswordGen.Core.History
{
    /// <summary>Cifratura dei dati riservati: in produzione DPAPI, nei test un'implementazione semplice.</summary>
    public interface ISecretProtector
    {
        byte[] Protect(byte[] data);

        byte[] Unprotect(byte[] data);
    }

#if NETFRAMEWORK
    /// <summary>
    /// DPAPI di Windows con ambito utente corrente: i dati si decifrano solo con lo stesso account Windows
    /// sullo stesso computer. Non protegge da un programma malevolo eseguito dallo stesso utente.
    /// </summary>
    public sealed class DpapiProtector : ISecretProtector
    {
        // Entropia aggiuntiva specifica dell'applicazione: altri programmi dello stesso utente non decifrano per caso il file.
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("PasswordGen.History.v1");

        public byte[] Protect(byte[] data)
        {
            return ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
        }

        public byte[] Unprotect(byte[] data)
        {
            return ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser);
        }
    }
#endif
}
