using PasswordGen.Core.History;

namespace PasswordGen.Mobile.Services;

/// <summary>
/// Chiave di cifratura dello storico, conservata con <see cref="SecureStorage"/> (Android Keystore): non sta mai nei file dell'app.
/// Se la chiave non si può né leggere né salvare, restituisce null e lo storico resta disattivato.
/// </summary>
public static class HistoryKey
{
    private const string Name = "passwordgen.history.key";

    public static byte[] GetOrCreate()
    {
        try
        {
            // Su un thread del pool: niente rischio di blocco con il thread dell'interfaccia.
            var stored = Task.Run(() => SecureStorage.Default.GetAsync(Name)).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(stored))
            {
                var bytes = Convert.FromBase64String(stored);
                if (bytes.Length == AesHmacProtector.KeyLength)
                {
                    return bytes;
                }
            }
        }
        catch (Exception)
        {
            // Chiave illeggibile (per esempio dopo un ripristino): se ne crea una nuova, lo storico precedente non è più leggibile.
        }

        try
        {
            var key = AesHmacProtector.GenerateKey();
            SecureStorage.Default.Remove(Name);
            Task.Run(() => SecureStorage.Default.SetAsync(Name, Convert.ToBase64String(key))).GetAwaiter().GetResult();
            return key;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
