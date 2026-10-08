namespace PasswordGen.Mobile.Services;

/// <summary>Promemoria del cambio password tramite notifiche di Android.</summary>
public interface IReminderScheduler
{
    /// <summary>Attiva o disattiva il controllo giornaliero (le impostazioni vanno salvate prima).</summary>
    void Apply(bool enabled);

    /// <summary>Su Android 13+ chiede il permesso di mostrare notifiche; restituisce true se è concesso.</summary>
    Task<bool> EnsureNotificationPermissionAsync();
}
