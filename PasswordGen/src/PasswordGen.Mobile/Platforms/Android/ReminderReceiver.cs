using Android.App;
using Android.Content;

namespace PasswordGen.Mobile;

/// <summary>Riceve l'allarme giornaliero e mostra la notifica se la password sta per scadere.</summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
public class ReminderReceiver : BroadcastReceiver
{
    public override void OnReceive(Context context, Intent intent)
    {
        try
        {
            ReminderNotifier.NotifyIfDue(context);
        }
        catch (Exception)
        {
            // Un errore nella notifica non deve far chiudere l'app.
        }
    }
}
