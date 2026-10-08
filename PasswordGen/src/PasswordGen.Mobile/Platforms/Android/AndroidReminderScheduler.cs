using Android.App;
using Android.Content;
using Microsoft.Maui.ApplicationModel;

namespace PasswordGen.Mobile.Services;

/// <summary>
/// Pianifica un controllo giornaliero (intorno alle 9:00) con un allarme inesatto di Android: non richiede il permesso
/// degli allarmi esatti e consuma poca batteria; l'orario può slittare di qualche minuto.
/// </summary>
public sealed class AndroidReminderScheduler : IReminderScheduler
{
    private const int RequestCode = 4711;

    public void Apply(bool enabled)
    {
        var context = Android.App.Application.Context;
        if (enabled)
        {
            Schedule(context);
        }
        else
        {
            Cancel(context);
        }
    }

    public async Task<bool> EnsureNotificationPermissionAsync()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return true;
        }

        var status = await Permissions.CheckStatusAsync<PostNotificationsPermission>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<PostNotificationsPermission>();
        }

        return status == PermissionStatus.Granted;
    }

    public static void Schedule(Context context)
    {
        var alarm = context.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarm == null)
        {
            return;
        }

        var first = DateTime.Today.AddHours(9);
        if (first <= DateTime.Now)
        {
            first = first.AddDays(1);
        }

        var triggerAt = new DateTimeOffset(first).ToUnixTimeMilliseconds();
        alarm.SetInexactRepeating(AlarmType.RtcWakeup, triggerAt, AlarmManager.IntervalDay, CreatePendingIntent(context));
    }

    public static void Cancel(Context context)
    {
        (context.GetSystemService(Context.AlarmService) as AlarmManager)?.Cancel(CreatePendingIntent(context));
    }

    private static PendingIntent CreatePendingIntent(Context context)
    {
        var intent = new Intent(context, typeof(ReminderReceiver));
        return PendingIntent.GetBroadcast(context, RequestCode, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
    }

    /// <summary>Permesso POST_NOTIFICATIONS (runtime da Android 13).</summary>
    private sealed class PostNotificationsPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            new[] { (ReminderNotifier.PostNotifications, true) };
    }
}
