using Android.App;
using Android.Content;
using PasswordGen.Mobile.Services;

namespace PasswordGen.Mobile;

/// <summary>Gli allarmi di Android si perdono al riavvio del telefono: li si ripianifica se il promemoria è attivo.</summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted })]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context context, Intent intent)
    {
        try
        {
            if (intent?.Action == Intent.ActionBootCompleted && ReminderNotifier.LoadSettings(context).ReminderEnabled)
            {
                AndroidReminderScheduler.Schedule(context);
            }
        }
        catch (Exception)
        {
            // Nulla da fare: si ripianifica alla prossima apertura dell'app.
        }
    }
}
