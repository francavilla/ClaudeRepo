using System;

namespace PasswordGen.Services
{
    /// <summary>Appunti per dati riservati: la password copiata viene cancellata dopo un po'.</summary>
    public interface ISecretClipboard
    {
        /// <summary>Dopo quanto tempo gli appunti vengono svuotati.</summary>
        TimeSpan ClearAfter { get; }

        /// <summary>Si verifica quando gli appunti sono stati svuotati automaticamente.</summary>
        event EventHandler Cleared;

        /// <summary>Copia il testo; restituisce false se gli appunti non sono disponibili.</summary>
        bool Copy(string text);

        /// <summary>Svuota subito gli appunti se contengono ancora l'ultima password copiata.</summary>
        void ClearIfPending();
    }
}
