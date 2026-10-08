using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui.Authentication;
using PasswordGen.Mobile.Services;

namespace PasswordGen.Mobile;

/// <summary>
/// Riceve il reindirizzamento di Google dopo l'accesso (indirizzo con lo schema personalizzato dell'app) e lo passa al WebAuthenticator di MAUI.
/// </summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = GoogleDriveService.CallbackScheme)]
public class GoogleCallbackActivity : WebAuthenticatorCallbackActivity
{
}
