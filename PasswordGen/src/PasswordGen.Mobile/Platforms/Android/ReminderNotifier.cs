using Android.App;
using Android.Content;
using Android.Content.PM;
using PasswordGen.Core.Reminder;
using PasswordGen.Core.Settings;

namespace PasswordGen.Mobile;

/// <summary>Controlla la scadenza e, se serve, mostra la notifica. Usa solo le API di Android: può girare anche a app chiusa.</summary>
public static class ReminderNotifier
{
    private const string ChannelId = "promemoria-password";
    private const int NotificationId = 1001;
    public const string PostNotifications = "android.permission.POST_NOTIFICATIONS";

    public static AppSettings LoadSettings(Context context)
    {
        // È la stessa cartella di FileSystem.AppDataDirectory, usata dall'app per salvare le impostazioni.
        return new SettingsStore(Path.Combine(context.FilesDir.AbsolutePath, "settings.json")).Load();
    }

    public static void NotifyIfDue(Context context)
    {
        var settings = LoadSettings(context);
        var state = ChangeReminder.Evaluate(settings.ReminderEnabled, settings.LastChangeDate, settings.ValidityDays, settings.WarnDays, DateTime.Today);
        if (!state.ShouldAlert)
        {
            return;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(33) && context.CheckSelfPermission(PostNotifications) != Permission.Granted)
        {
            return;
        }

        var manager = context.GetSystemService(Context.NotificationService) as NotificationManager;
        if (manager == null)
        {
            return;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var channel = new NotificationChannel(ChannelId, "Promemoria cambio password", NotificationImportance.Default)
            {
                Description = "Avvisa quando la password sta per scadere.",
            };
            manager.CreateNotificationChannel(channel);
        }

        var open = PendingIntent.GetActivity(
            context, 0, new Intent(context, typeof(MainActivity)), PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

#pragma warning disable CA1422 // costruttore senza canale: serve solo prima di Android 8
        var builder = OperatingSystem.IsAndroidVersionAtLeast(26)
            ? new Notification.Builder(context, ChannelId)
            : new Notification.Builder(context);
#pragma warning restore CA1422

        builder.SetContentTitle("PasswordGen")
            .SetContentText(state.Message)
            .SetSmallIcon(context.ApplicationInfo.Icon)
            .SetContentIntent(open)
            .SetAutoCancel(true);

        manager.Notify(NotificationId, builder.Build());
    }
}
