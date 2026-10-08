using Android.App;
using Android.Content;
using Android.Hardware.Biometrics;
using Android.OS;
using Android.Views;
using System.Runtime.Versioning;
using Microsoft.Maui.ApplicationModel;

namespace PasswordGen.Mobile.Services;

/// <summary>
/// Autenticazione con le finestre di sistema di Android: impronta o volto (<see cref="BiometricPrompt"/>) e, in alternativa, la
/// schermata del telefono per PIN, sequenza o password. L'app non vede mai le credenziali, riceve solo l'esito.
/// </summary>
public sealed class AndroidSecurityService : ISecurityService
{
    private const int CredentialRequestCode = 4721;
    private const int BiometricWeak = 255;

    // Codici di errore dopo i quali ha senso passare al PIN: sensore assente, non registrato, non disponibile, bloccato.
    private static readonly int[] FallbackErrors = { 1, 7, 9, 11, 12 };

    private static TaskCompletionSource<AuthenticationOutcome> _credentialResult;
    private static bool _authenticating;

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

    public bool IsAuthenticating => _authenticating;

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

        _authenticating = true;
        result.Task.ContinueWith(_ => _authenticating = false);

        // Passa alla schermata del PIN; l'esito di quella schermata diventa l'esito complessivo.
        void UseCredential()
        {
            AuthenticateWithDeviceCredentialAsync(title, subtitle).ContinueWith(
                t => result.TrySetResult(t.IsCompletedSuccessfully ? t.Result : AuthenticationOutcome.Failed("errore del sistema")));
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // Il controllo è qui, dentro la funzione: l'analisi del compilatore non lo "vede" se sta fuori.
                if (OperatingSystem.IsAndroidVersionAtLeast(28))
                {
                    ShowBiometricPrompt(activity, title, subtitle, result, UseCredential);
                }
                else
                {
                    UseCredential();
                }
            }
            catch (Exception)
            {
                // La finestra biometrica non si apre: si passa direttamente al PIN.
                UseCredential();
            }
        });

        return result.Task;
    }

    /// <summary>Finestra di sistema per impronta o volto: esiste solo da Android 9 (livello 28).</summary>
    [SupportedOSPlatform("android28.0")]
    private static void ShowBiometricPrompt(Activity activity, string title, string subtitle,
        TaskCompletionSource<AuthenticationOutcome> result, Action useCredential)
    {
        var builder = new BiometricPrompt.Builder(activity)
            .SetTitle(title)
            .SetSubtitle(subtitle)
            .SetNegativeButton("Usa PIN o password", activity.MainExecutor, new CredentialClickListener(useCredential));

        // Dal Android 11 si dichiara che qui si accettano solo i dati biometrici: il PIN è il pulsante sopra.
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            builder.SetAllowedAuthenticators(BiometricWeak);
        }

        builder.Build().Authenticate(new CancellationSignal(), activity.MainExecutor, new BiometricResultCallback(result, useCredential));
    }

    public Task<AuthenticationOutcome> AuthenticateWithDeviceCredentialAsync(string title, string subtitle)
    {
        var result = new TaskCompletionSource<AuthenticationOutcome>();
        var activity = Platform.CurrentActivity;
        var keyguard = Android.App.Application.Context.GetSystemService(Context.KeyguardService) as KeyguardManager;
        var intent = keyguard?.CreateConfirmDeviceCredentialIntent(title, subtitle);
        if (activity == null || intent == null)
        {
            result.TrySetResult(AuthenticationOutcome.Failed("il telefono non ha un PIN, una sequenza o una password impostati"));
            return result.Task;
        }

        _authenticating = true;
        result.Task.ContinueWith(_ => _authenticating = false);
        _credentialResult = result;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                activity.StartActivityForResult(intent, CredentialRequestCode);
            }
            catch (Exception ex)
            {
                _credentialResult = null;
                result.TrySetResult(AuthenticationOutcome.Failed("errore del sistema: " + ex.Message));
            }
        });

        return result.Task;
    }

    /// <summary>Da chiamare da <c>MainActivity.OnActivityResult</c>: riceve l'esito della schermata del PIN.</summary>
    public static void OnActivityResult(int requestCode, Result resultCode)
    {
        if (requestCode != CredentialRequestCode)
        {
            return;
        }

        var pending = _credentialResult;
        _credentialResult = null;
        pending?.TrySetResult(resultCode == Result.Ok ? AuthenticationOutcome.Ok : AuthenticationOutcome.Failed("verifica annullata"));
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

    internal static bool ShouldFallBack(int errorCode) => Array.IndexOf(FallbackErrors, errorCode) >= 0;
}

/// <summary>Pulsante «Usa PIN o password» della finestra dell'impronta.</summary>
internal sealed class CredentialClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
{
    private readonly Action _onClick;

    public CredentialClickListener(Action onClick)
    {
        _onClick = onClick;
    }

    public void OnClick(IDialogInterface dialog, int which)
    {
        _onClick();
    }
}

[SupportedOSPlatform("android28.0")]
internal sealed class BiometricResultCallback : BiometricPrompt.AuthenticationCallback
{
    private const int NegativeButton = 13;

    private readonly TaskCompletionSource<AuthenticationOutcome> _result;
    private readonly Action _useCredential;

    public BiometricResultCallback(TaskCompletionSource<AuthenticationOutcome> result, Action useCredential)
    {
        _result = result;
        _useCredential = useCredential;
    }

    public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
    {
        _result.TrySetResult(AuthenticationOutcome.Ok);
    }

    public override void OnAuthenticationError(BiometricErrorCode errorCode, Java.Lang.ICharSequence errString)
    {
        var code = (int)errorCode;

        // Il pulsante «Usa PIN o password» ha già avviato la schermata del PIN: l'esito arriverà da lì.
        if (code == NegativeButton)
        {
            return;
        }

        // Impronta non utilizzabile (non registrata, sensore occupato, troppi tentativi): si passa al PIN.
        if (AndroidSecurityService.ShouldFallBack(code))
        {
            _useCredential();
            return;
        }

        // Annullamento o altro: l'app resta chiusa. Si riporta il motivo del sistema.
        var text = errString?.ToString();
        _result.TrySetResult(AuthenticationOutcome.Failed(
            string.IsNullOrWhiteSpace(text) ? "codice " + code : text + " (codice " + code + ")"));
    }
}
