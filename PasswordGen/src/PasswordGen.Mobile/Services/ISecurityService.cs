namespace PasswordGen.Mobile.Services;

/// <summary>Funzioni di sicurezza del sistema: autenticazione (impronta, volto, PIN) e protezione dalle schermate.</summary>
public interface ISecurityService
{
    /// <summary>True se il telefono ha un blocco schermo (PIN, sequenza o password) e Android è abbastanza recente (9 o successivo).</summary>
    bool IsAvailable { get; }

    /// <summary>Chiede impronta, volto o PIN del telefono; restituisce true se l'utente si è autenticato.</summary>
    Task<bool> AuthenticateAsync(string title, string subtitle);

    /// <summary>Impedisce screenshot, registrazioni dello schermo e anteprima dell'app tra le app recenti.</summary>
    void SetScreenCaptureBlocked(bool blocked);
}
