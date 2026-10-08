using System.Net.Http;
using Microsoft.Maui.Authentication;
using PasswordGen.Core.Sync;
using PasswordGen.Core.Sync.Google;

namespace PasswordGen.Mobile.Services;

/// <summary>
/// Accesso a Google Drive con OAuth 2.0 per app Android: il browser del telefono mostra la richiesta di Google, l'app riceve un codice
/// (schema di reindirizzamento personalizzato) e lo scambia con i token. Il token di rinnovo resta nel Keystore; l'app non vede mai la password.
/// </summary>
public sealed class GoogleDriveService : IGoogleDriveService
{
    // Identificativo pubblico del client OAuth di tipo Android (non è un segreto).
    public const string ClientId = "639985368800-iptpjhv0k367jjplio2p9q5eadr6mm9g.apps.googleusercontent.com";

    /// <summary>Schema di reindirizzamento: l'ID client letto al contrario, come vuole Google per i client Android.</summary>
    public const string CallbackScheme = "com.googleusercontent.apps.639985368800-iptpjhv0k367jjplio2p9q5eadr6mm9g";

    public const string RedirectUri = CallbackScheme + ":/oauth2redirect";

    private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

    private readonly GoogleOAuthClient _oauth;
    private readonly GoogleAccessTokenProvider _tokens;

    /// <param name="refreshTokens">Archivio cifrato del token di rinnovo; nullo se il Keystore non ha dato la chiave.</param>
    public GoogleDriveService(SyncPassphraseStore refreshTokens)
    {
        _oauth = new GoogleOAuthClient(Http, ClientId, RedirectUri);
        _tokens = refreshTokens == null ? null : new GoogleAccessTokenProvider(_oauth, refreshTokens);
    }

    public string Address => "google-drive:";

    public bool IsSignedIn => _tokens != null && _tokens.IsSignedIn;

    public async Task<string> SignInAsync()
    {
        if (_tokens == null)
        {
            return "L'accesso a Google non è disponibile: il Keystore di Android non ha dato la chiave di cifratura.";
        }

        var pkce = GoogleOAuthClient.CreatePkce();
        var options = new WebAuthenticatorOptions
        {
            Url = _oauth.BuildAuthorizationUri(pkce),
            CallbackUrl = new Uri(RedirectUri),
        };

        // Il browser porta l'app in secondo piano: non deve far scattare il blocco.
        ExternalActivity.IsActive = true;
        try
        {
            var result = await WebAuthenticator.Default.AuthenticateAsync(options);
            result.Properties.TryGetValue("error", out var error);
            if (!string.IsNullOrEmpty(error))
            {
                return error == "access_denied" ? "Accesso negato: non hai consentito l'uso di Google Drive." : "Google ha risposto con un errore: " + error + ".";
            }

            result.Properties.TryGetValue("state", out var state);
            result.Properties.TryGetValue("code", out var code);
            if (state != pkce.State || string.IsNullOrEmpty(code))
            {
                return "Risposta di Google non valida: riprova l'accesso.";
            }

            var tokens = await _oauth.ExchangeCodeAsync(code, pkce.Verifier);
            _tokens.SignIn(tokens);
            return null;
        }
        catch (TaskCanceledException)
        {
            return "Accesso annullato.";
        }
        catch (Exception ex)
        {
            return "Accesso a Google non riuscito: " + ex.Message;
        }
        finally
        {
            ExternalActivity.IsActive = false;
        }
    }

    public ISyncStorage CreateStorage()
    {
        if (_tokens == null)
        {
            throw new InvalidOperationException("Accesso a Google non disponibile.");
        }

        return new GoogleDriveStorage(Http, _tokens);
    }

    public void SignOut()
    {
        _tokens?.SignOut();
    }
}
