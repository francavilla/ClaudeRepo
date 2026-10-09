using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PasswordGen.Core.Sync;
using PasswordGen.Core.Sync.Google;

namespace PasswordGen.Services
{
    /// <summary>
    /// Accesso a Google Drive con OAuth 2.0 per app per computer: il browser predefinito mostra la richiesta di Google, il ritorno arriva a un piccolo
    /// server locale (127.0.0.1) e il codice si scambia con i token (PKCE). L'app non vede mai la password di Google; il token di rinnovo è cifrato con DPAPI.
    /// L'ID client (e la chiave del client, che per le app per computer Google non considera riservata) sono incorporati in fase di compilazione.
    /// </summary>
    public sealed class GoogleDriveService : IGoogleDriveService
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly GoogleOAuthClient _oauth;
        private readonly GoogleAccessTokenProvider _tokens;

        public GoogleDriveService(SyncPassphraseStore refreshTokens)
            : this(refreshTokens, ReadMetadata("GoogleClientId"), ReadMetadata("GoogleClientSecret"))
        {
        }

        public GoogleDriveService(SyncPassphraseStore refreshTokens, string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _oauth = new GoogleOAuthClient(Http, clientId ?? string.Empty, "http://127.0.0.1/", null, clientSecret);
            _tokens = new GoogleAccessTokenProvider(_oauth, refreshTokens);
        }

        public string Address
        {
            get { return "google-drive:"; }
        }

        public bool IsConfigured
        {
            get { return !string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret); }
        }

        public bool IsSignedIn
        {
            get { return IsConfigured && _tokens.IsSignedIn; }
        }

        public async Task<string> SignInAsync()
        {
            if (!IsConfigured)
            {
                return "Google Drive non è configurato in questa versione del programma (manca l'ID client o la sua chiave).";
            }

            var pkce = GoogleOAuthClient.CreatePkce();

            // Il browser porta l'attenzione fuori dall'app: non deve far scattare il blocco.
            ExternalActivity.Enter();
            try
            {
                using (var receiver = new LoopbackReceiver())
                {
                    var uri = _oauth.BuildAuthorizationUri(pkce, receiver.RedirectUri);
                    Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });

                    var parameters = await receiver.WaitAsync(TimeSpan.FromMinutes(3), CancellationToken.None);
                    if (parameters == null)
                    {
                        return "Accesso scaduto: da Google non è tornata nessuna risposta in 3 minuti. Riprova.";
                    }

                    string error;
                    if (parameters.TryGetValue("error", out error) && !string.IsNullOrEmpty(error))
                    {
                        return error == "access_denied"
                            ? "Accesso negato: non hai consentito l'uso di Google Drive."
                            : "Google ha risposto con un errore: " + error + ".";
                    }

                    string state;
                    string code;
                    parameters.TryGetValue("state", out state);
                    parameters.TryGetValue("code", out code);
                    if (state != pkce.State || string.IsNullOrEmpty(code))
                    {
                        return "Risposta di Google non valida: riprova l'accesso.";
                    }

                    var tokens = await _oauth.ExchangeCodeAsync(code, pkce.Verifier, receiver.RedirectUri);
                    _tokens.SignIn(tokens);
                    return null;
                }
            }
            catch (Exception ex)
            {
                return "Accesso a Google non riuscito: " + ex.Message;
            }
            finally
            {
                ExternalActivity.Leave();
            }
        }

        public ISyncStorage CreateStorage()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException("Google Drive non è configurato in questa versione del programma.");
            }

            return new GoogleDriveStorage(Http, _tokens);
        }

        public void SignOut()
        {
            _tokens.SignOut();
        }

        private static string ReadMetadata(string key)
        {
            var attribute = typeof(GoogleDriveService).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == key);
            return attribute == null ? null : attribute.Value;
        }
    }
}
