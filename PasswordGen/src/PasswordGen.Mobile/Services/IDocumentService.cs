using PasswordGen.Core.Sync;

namespace PasswordGen.Mobile.Services;

/// <summary>
/// Scelta e uso di un documento dell'utente (per esempio nella cartella di Google Drive) tramite il selettore di sistema.
/// Il documento è identificato da un indirizzo (stringa) che l'app conserva; l'accesso resta valido anche dopo la chiusura dell'app.
/// </summary>
public interface IDocumentService
{
    /// <summary>Fa scegliere un file esistente; restituisce il suo indirizzo, o null se l'utente annulla.</summary>
    Task<string> PickExistingAsync();

    /// <summary>Fa creare un nuovo file (l'utente sceglie la cartella); restituisce il suo indirizzo, o null se annulla.</summary>
    Task<string> CreateAsync(string suggestedName);

    /// <summary>Lettura e scrittura del documento.</summary>
    ISyncStorage Open(string address);

    /// <summary>Nome leggibile del documento (per mostrarlo all'utente).</summary>
    string DisplayName(string address);
}
