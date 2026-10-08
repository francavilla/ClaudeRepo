namespace PasswordGen.Mobile.Services;

/// <summary>Funzioni di sicurezza del sistema: autenticazione (impronta, volto, PIN) e protezione dalle schermate.</summary>
public interface ISecurityService
{
    /// <summary>True se il telefono ha un blocco schermo (PIN, sequenza o password) e Android è abbastanza recente (9 o successivo).</summary>
    bool IsAvailable { get; }

    /// <summary>True mentre è aperta una richiesta di autenticazione (l'app esce dal primo piano e rientra: non va contato come uscita).</summary>
    bool IsAuthenticating { get; }

    /// <summary>
    /// Chiede impronta o volto, con il pulsante «Usa PIN o password» per ripiegare sulle credenziali del telefono; se i dati biometrici non
    /// sono utilizzabili passa da sola al PIN. L'esito dice se l'utente si è autenticato e, se no, perché.
    /// </summary>
    Task<AuthenticationOutcome> AuthenticateAsync(string title, string subtitle);

    /// <summary>Apre direttamente la schermata del telefono per PIN, sequenza o password.</summary>
    Task<AuthenticationOutcome> AuthenticateWithDeviceCredentialAsync(string title, string subtitle);

    /// <summary>Impedisce screenshot, registrazioni dello schermo e anteprima dell'app tra le app recenti.</summary>
    void SetScreenCaptureBlocked(bool blocked);
}

/// <summary>Esito di un'autenticazione.</summary>
public sealed class AuthenticationOutcome
{
    private AuthenticationOutcome(bool success, string reason)
    {
        Success = success;
        Reason = reason;
    }

    public bool Success { get; }

    /// <summary>Motivo del mancato accesso, in italiano (vuoto se l'autenticazione è riuscita).</summary>
    public string Reason { get; }

    public static AuthenticationOutcome Ok { get; } = new AuthenticationOutcome(true, string.Empty);

    public static AuthenticationOutcome Failed(string reason) => new AuthenticationOutcome(false, reason);
}
