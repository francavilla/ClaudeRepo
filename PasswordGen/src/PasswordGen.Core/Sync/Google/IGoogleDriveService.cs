using System.Threading.Tasks;

namespace PasswordGen.Core.Sync.Google
{
    /// <summary>Accesso al Google Drive dell'utente per la sincronizzazione (account Google, nessuna cartella locale). Lo realizzano le due app.</summary>
    public interface IGoogleDriveService
    {
        /// <summary>Indirizzo con cui la sincronizzazione su Drive viene memorizzata nelle impostazioni.</summary>
        string Address { get; }

        /// <summary>False se l'app non ha un client OAuth configurato (manca l'ID client): l'opzione Google Drive non è utilizzabile.</summary>
        bool IsConfigured { get; }

        bool IsSignedIn { get; }

        /// <summary>Apre l'accesso a Google nel browser; restituisce null se è andato a buon fine, altrimenti il motivo.</summary>
        Task<string> SignInAsync();

        /// <summary>Il file di sincronizzazione su Drive (chiamate sincrone: da usare fuori dal thread dell'interfaccia).</summary>
        ISyncStorage CreateStorage();

        /// <summary>Dimentica l'accesso su questo dispositivo (il file su Drive non viene toccato).</summary>
        void SignOut();
    }
}
