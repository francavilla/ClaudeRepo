using Android.Content;
using Android.OS;
using Microsoft.Maui.ApplicationModel;

namespace PasswordGen.Mobile.Services;

/// <summary>
/// Copia la password negli appunti segnalandola come sensibile (Android 13+: non compare nell'anteprima degli appunti)
/// e prova a cancellarla dopo <see cref="ClearAfter"/>. Dall'Android 10 un'app in secondo piano non può sempre leggere
/// gli appunti: in quel caso la cancellazione avviene comunque.
/// </summary>
public sealed class SecretClipboard
{
    private CancellationTokenSource _pending;
    private string _text;

    public SecretClipboard(TimeSpan clearAfter)
    {
        ClearAfter = clearAfter;
    }

    public TimeSpan ClearAfter { get; }

    public bool Copy(string text)
    {
        try
        {
            var manager = GetManager();
            if (manager == null)
            {
                return false;
            }

            var clip = ClipData.NewPlainText("password", text);
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                var extras = new PersistableBundle();
                extras.PutBoolean("android.content.extra.IS_SENSITIVE", true);
                clip.Description.Extras = extras;
            }

            manager.PrimaryClip = clip;
            ScheduleClear(text);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void ScheduleClear(string text)
    {
        _pending?.Cancel();
        _pending = new CancellationTokenSource();
        _text = text;
        var token = _pending.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(ClearAfter, token);
                MainThread.BeginInvokeOnMainThread(ClearIfStillOurs);
            }
            catch (OperationCanceledException)
            {
                // Una nuova copia ha sostituito questa.
            }
        });
    }

    private void ClearIfStillOurs()
    {
        try
        {
            var manager = GetManager();
            if (manager == null)
            {
                return;
            }

            // Se non si riesce a leggere (app in secondo piano) si svuota comunque.
            var current = manager.PrimaryClip?.GetItemAt(0)?.Text;
            if (current != null && current != _text)
            {
                return;
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                manager.ClearPrimaryClip();
            }
            else
            {
                manager.PrimaryClip = ClipData.NewPlainText(string.Empty, string.Empty);
            }
        }
        catch (Exception)
        {
            // Appunti non accessibili in questo momento: non si può fare altro.
        }
    }

    private static ClipboardManager GetManager()
    {
        return Android.App.Application.Context.GetSystemService(Context.ClipboardService) as ClipboardManager;
    }
}
