using PasswordGen.Core.Sync;

namespace PasswordGen.Mobile.Services;

/// <summary>Accesso al Google Drive dell'utente per la sincronizzazione (account Google, nessuna cartella locale).</summary>
public interface IGoogleDriveService
{
    /// <summary>Indirizzo con cui la sincronizzazione su Drive viene memorizzata nelle impostazioni.</summary>
    string Address { get; }

    bool IsSignedIn { get; }

    /// <summary>Apre l'accesso a Google nel browser; restituisce null se è andato a buon fine, altrimenti il motivo.</summary>
    Task<string> SignInAsync();

    /// <summary>Il file di sincronizzazione su Drive (chiamate sincrone: da usare fuori dal thread dell'interfaccia).</summary>
    ISyncStorage CreateStorage();

    /// <summary>Dimentica l'accesso su questo telefono (il file su Drive non viene toccato).</summary>
    void SignOut();
}
