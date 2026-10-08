using Android.App;
using Android.Content;
using Android.Hardware.Biometrics;
using Android.OS;
using Android.Views;
using Microsoft.Maui.ApplicationModel;

namespace PasswordGen.Mobile.Services;

/// <summary>
/// Autenticazione con la finestra di sistema di Android (<see cref="BiometricPrompt"/>): impronta, volto oppure PIN, sequenza
/// o password del telefono. L'app non vede mai le credenziali, riceve solo l'esito.
/// </summary>
public sealed class AndroidSecurityService : ISecurityService
{
    // BIOMETRIC_WEAK | DEVICE_CREDENTIAL (Android 11+); prima si usa la modalità "credenziale del dispositivo consentita".
    private const int Authenticators = 255 | 32768;

    public bool IsAvailable
    {
        get
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                return false;
            }

            var keyguard = Android.App.Application.Context.GetSystemService(Context.KeyguardService) as KeyguardManager;
            return keyguard != null && keyguard.IsDeviceSecure;
        }
    }

    public Task<AuthenticationOutcome> AuthenticateAsync(string title, string subtitle)
    {
        var result = new TaskCompletionSource<AuthenticationOutcome>();
        var activity = Platform.CurrentActivity;
        if (!OperatingSystem.IsAndroidVersionAtLeast(28))
        {
            result.TrySetResult(AuthenticationOutcome.Failed("serve Android 9 o successivo"));
            return result.Task;
        }

        if (activity == null)
        {
            result.TrySetResult(AuthenticationOutcome.Failed("nessuna schermata attiva a cui mostrare la richiesta"));
            return result.Task;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var builder = new BiometricPrompt.Builder(activity).SetTitle(title).SetSubtitle(subtitle);
                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                {
                    builder.SetAllowedAuthenticators(Authenticators);
                }
                else
                {
#pragma warning disable CA1422 // sostituito da SetAllowedAuthenticators su Android 11+
                    builder.SetDeviceCredentialAllowed(true);
#pragma warning restore CA1422
                }

                builder.Build().Authenticate(new CancellationSignal(), activity.MainExecutor, new BiometricResultCallback(result));
            }
            catch (Exception ex)
            {
                result.TrySetResult(AuthenticationOutcome.Failed("errore del sistema: " + ex.Message));
            }
        });

        return result.Task;
    }

    public void SetScreenCaptureBlocked(bool blocked)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var window = Platform.CurrentActivity?.Window;
            if (window == null)
            {
                return;
            }

            if (blocked)
            {
                window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
            }
            else
            {
                window.ClearFlags(WindowManagerFlags.Secure);
            }
        });
    }
}

internal sealed class BiometricResultCallback : BiometricPrompt.AuthenticationCallback
{
    private readonly TaskCompletionSource<AuthenticationOutcome> _result;

    public BiometricResultCallback(TaskCompletionSource<AuthenticationOutcome> result)
    {
        _result = result;
    }

    public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
    {
        _result.TrySetResult(AuthenticationOutcome.Ok);
    }

    public override void OnAuthenticationError(BiometricErrorCode errorCode, Java.Lang.ICharSequence errString)
    {
        // Annullamento, troppi tentativi, blocco temporaneo: in ogni caso l'app resta chiusa. Si riporta il motivo del sistema.
        var text = errString?.ToString();
        _result.TrySetResult(AuthenticationOutcome.Failed(string.IsNullOrWhiteSpace(text) ? "codice " + (int)errorCode : text + " (codice " + (int)errorCode + ")"));
    }
}
