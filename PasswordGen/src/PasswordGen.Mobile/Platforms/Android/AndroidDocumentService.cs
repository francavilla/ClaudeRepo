using Android.App;
using Android.Content;
using Android.Provider;
using Microsoft.Maui.ApplicationModel;
using PasswordGen.Core.Sync;

namespace PasswordGen.Mobile.Services;

/// <summary>
/// File dell'utente tramite il selettore di documenti di Android (Storage Access Framework): funziona con Google Drive e con
/// qualsiasi altra app che offre file. L'app non ha bisogno di permessi sull'archivio, ottiene l'accesso solo al file scelto.
/// </summary>
public sealed class AndroidDocumentService : IDocumentService
{
    private const int OpenRequestCode = 4731;
    private const int CreateRequestCode = 4732;

    // Valore di OpenableColumns.DisplayName (la classe è obsoleta e la sostituta non espone la costante).
    private const string DisplayNameColumn = "_display_name";

    private static TaskCompletionSource<string> _pending;
    private static int _pendingCode;

    public Task<string> PickExistingAsync()
    {
        return StartAsync(OpenRequestCode, () =>
        {
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");
            return intent;
        });
    }

    public Task<string> CreateAsync(string suggestedName)
    {
        return StartAsync(CreateRequestCode, () =>
        {
            var intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("application/octet-stream");
            intent.PutExtra(Intent.ExtraTitle, suggestedName);
            return intent;
        });
    }

    private static Task<string> StartAsync(int code, Func<Intent> build)
    {
        var result = new TaskCompletionSource<string>();
        var activity = Platform.CurrentActivity;
        if (activity == null)
        {
            result.TrySetResult(null);
            return result.Task;
        }

        var intent = build();
        intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPersistableUriPermission);

        ExternalActivity.IsActive = true;
        _pending = result;
        _pendingCode = code;
        try
        {
            activity.StartActivityForResult(intent, code);
        }
        catch (Exception)
        {
            ExternalActivity.IsActive = false;
            _pending = null;
            result.TrySetResult(null);
        }

        return result.Task;
    }

    /// <summary>Da chiamare da <c>MainActivity.OnActivityResult</c>: riceve il documento scelto.</summary>
    public static void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
        if (requestCode != OpenRequestCode && requestCode != CreateRequestCode)
        {
            return;
        }

        ExternalActivity.IsActive = false;
        var pending = _pending;
        if (pending == null || requestCode != _pendingCode)
        {
            return;
        }

        _pending = null;
        var uri = resultCode == Result.Ok ? data?.Data : null;
        if (uri != null)
        {
            try
            {
                // L'accesso al file deve sopravvivere alla chiusura dell'app (la sincronizzazione riusa lo stesso file).
                Android.App.Application.Context.ContentResolver.TakePersistableUriPermission(
                    uri, ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
            }
            catch (Exception)
            {
                // Alcuni fornitori non permettono accessi persistenti: il file funziona comunque finché l'app resta aperta.
            }
        }

        pending.TrySetResult(uri?.ToString());
    }

    public ISyncStorage Open(string address)
    {
        return new ContentUriSyncStorage(address);
    }

    public string DisplayName(string address)
    {
        try
        {
            var uri = Android.Net.Uri.Parse(address);
            using var cursor = Android.App.Application.Context.ContentResolver.Query(uri, new[] { DisplayNameColumn }, null, null, null);
            if (cursor != null && cursor.MoveToFirst())
            {
                var name = cursor.GetString(0);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            return uri.LastPathSegment ?? address;
        }
        catch (Exception)
        {
            return address;
        }
    }
}

/// <summary>Lettura e scrittura di un documento Android tramite il suo indirizzo content://.</summary>
internal sealed class ContentUriSyncStorage : ISyncStorage
{
    private readonly Android.Net.Uri _uri;

    public ContentUriSyncStorage(string address)
    {
        _uri = Android.Net.Uri.Parse(address);
    }

    public byte[] Read()
    {
        try
        {
            using var input = Android.App.Application.Context.ContentResolver.OpenInputStream(_uri);
            if (input == null)
            {
                return null;
            }

            using var memory = new MemoryStream();
            input.CopyTo(memory);
            return memory.ToArray();
        }
        catch (Java.IO.FileNotFoundException)
        {
            return null;
        }
    }

    public void Write(byte[] data)
    {
        // "wt": scrive da capo, svuotando il contenuto precedente.
        using var output = Android.App.Application.Context.ContentResolver.OpenOutputStream(_uri, "wt");
        if (output == null)
        {
            throw new IOException("Il documento non è scrivibile.");
        }

        output.Write(data, 0, data.Length);
        output.Flush();
    }
}
