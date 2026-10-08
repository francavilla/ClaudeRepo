using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PasswordGen.Core.Sync.Google
{
    /// <summary>Il programma deve fare di nuovo l'accesso a Google (accesso revocato, scaduto o mai eseguito).</summary>
    public sealed class GoogleAuthException : Exception
    {
        public GoogleAuthException(string message) : base(message)
        {
        }
    }

    /// <summary>Dati temporanei dell'accesso con PKCE: servono tra l'apertura del browser e lo scambio del codice.</summary>
    public sealed class PkceChallenge
    {
        public string Verifier { get; set; }
        public string Challenge { get; set; }
        public string State { get; set; }
    }

    public sealed class GoogleTokens
    {
        public string AccessToken { get; set; }

        /// <summary>Può mancare nelle risposte a un rinnovo: in quel caso resta valido quello già salvato.</summary>
        public string RefreshToken { get; set; }

        public DateTime ExpiresAtUtc { get; set; }
    }

    [DataContract]
    internal sealed class TokenResponse
    {
        [DataMember(Name = "access_token")] public string AccessToken { get; set; }
        [DataMember(Name = "refresh_token")] public string RefreshToken { get; set; }
        [DataMember(Name = "expires_in")] public int ExpiresIn { get; set; }
        [DataMember(Name = "error")] public string Error { get; set; }
        [DataMember(Name = "error_description")] public string ErrorDescription { get; set; }
    }

    /// <summary>
    /// Accesso a Google con OAuth 2.0 per app installate (PKCE, nessuna chiave segreta nel programma): costruisce l'indirizzo di accesso,
    /// scambia il codice con i token e rinnova l'accesso. Il browser lo apre chi chiama.
    /// </summary>
    public sealed class GoogleOAuthClient
    {
        public const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        public const string TokenEndpoint = "https://oauth2.googleapis.com/token";

        /// <summary>Accesso solo ai file creati da questa app: non vede gli altri file dell'utente.</summary>
        public const string DriveFileScope = "https://www.googleapis.com/auth/drive.file";

        private readonly HttpClient _http;
        private readonly string _clientId;
        private readonly string _redirectUri;
        private readonly Func<DateTime> _utcNow;

        public GoogleOAuthClient(HttpClient http, string clientId, string redirectUri, Func<DateTime> utcNow = null)
        {
            _http = http;
            _clientId = clientId;
            _redirectUri = redirectUri;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public static PkceChallenge CreatePkce()
        {
            var verifier = RandomUrlSafe(48);   // 64 caratteri (il minimo di Google è 43)
            using (var sha = SHA256.Create())
            {
                return new PkceChallenge
                {
                    Verifier = verifier,
                    Challenge = Base64Url(sha.ComputeHash(Encoding.ASCII.GetBytes(verifier))),
                    State = RandomUrlSafe(16)
                };
            }
        }

        public Uri BuildAuthorizationUri(PkceChallenge pkce)
        {
            var query = new[]
            {
                Pair("client_id", _clientId),
                Pair("redirect_uri", _redirectUri),
                Pair("response_type", "code"),
                Pair("scope", DriveFileScope),
                Pair("code_challenge", pkce.Challenge),
                Pair("code_challenge_method", "S256"),
                Pair("state", pkce.State),
                // offline + consent: Google restituisce il token di rinnovo, così l'accesso dura senza chiedere ogni volta.
                Pair("access_type", "offline"),
                Pair("prompt", "consent")
            };
            return new Uri(AuthEndpoint + "?" + string.Join("&", query));
        }

        public async Task<GoogleTokens> ExchangeCodeAsync(string code, string verifier)
        {
            return await PostAsync(new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "code", code },
                { "code_verifier", verifier },
                { "grant_type", "authorization_code" },
                { "redirect_uri", _redirectUri }
            });
        }

        /// <exception cref="GoogleAuthException">Il rinnovo è stato rifiutato: serve un nuovo accesso.</exception>
        public async Task<GoogleTokens> RefreshAsync(string refreshToken)
        {
            return await PostAsync(new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" }
            });
        }

        private async Task<GoogleTokens> PostAsync(Dictionary<string, string> form)
        {
            using (var response = await _http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form)).ConfigureAwait(false))
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                TokenResponse parsed = null;
                try
                {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(body)))
                    {
                        parsed = (TokenResponse)new DataContractJsonSerializer(typeof(TokenResponse)).ReadObject(stream);
                    }
                }
                catch (Exception)
                {
                    // Risposta non JSON: si gestisce sotto con il codice HTTP.
                }

                if (!response.IsSuccessStatusCode || parsed == null || string.IsNullOrEmpty(parsed.AccessToken))
                {
                    if (parsed != null && (parsed.Error == "invalid_grant" || parsed.Error == "invalid_client" || parsed.Error == "unauthorized_client"))
                    {
                        throw new GoogleAuthException("L'accesso a Google è scaduto o è stato revocato: accedi di nuovo.");
                    }

                    var detail = parsed != null && !string.IsNullOrEmpty(parsed.Error)
                        ? parsed.Error + (string.IsNullOrEmpty(parsed.ErrorDescription) ? string.Empty : " (" + parsed.ErrorDescription + ")")
                        : "HTTP " + (int)response.StatusCode;
                    throw new IOException("Google ha rifiutato la richiesta di accesso: " + detail + ".");
                }

                return new GoogleTokens
                {
                    AccessToken = parsed.AccessToken,
                    RefreshToken = parsed.RefreshToken,
                    ExpiresAtUtc = _utcNow().AddSeconds(parsed.ExpiresIn > 0 ? parsed.ExpiresIn : 3600)
                };
            }
        }

        private static string Pair(string name, string value)
        {
            return Uri.EscapeDataString(name) + "=" + Uri.EscapeDataString(value);
        }

        private static string RandomUrlSafe(int byteCount)
        {
            var bytes = new byte[byteCount];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return Base64Url(bytes);
        }

        private static string Base64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }

    /// <summary>
    /// Tiene il token di accesso (breve, ~1 ora) in memoria e lo rinnova col token di rinnovo, conservato cifrato sul dispositivo.
    /// </summary>
    public sealed class GoogleAccessTokenProvider
    {
        private readonly GoogleOAuthClient _oauth;
        private readonly SyncPassphraseStore _refreshTokens;
        private readonly Func<DateTime> _utcNow;
        private readonly object _lock = new object();
        private GoogleTokens _cached;

        /// <param name="refreshTokens">Archivio cifrato di un solo testo (lo stesso della frase segreta, in un file diverso).</param>
        public GoogleAccessTokenProvider(GoogleOAuthClient oauth, SyncPassphraseStore refreshTokens, Func<DateTime> utcNow = null)
        {
            _oauth = oauth;
            _refreshTokens = refreshTokens;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public bool IsSignedIn
        {
            get { return _refreshTokens.Load() != null; }
        }

        /// <summary>Registra l'esito di un accesso appena eseguito.</summary>
        public void SignIn(GoogleTokens tokens)
        {
            if (string.IsNullOrEmpty(tokens.RefreshToken))
            {
                throw new GoogleAuthException("Google non ha restituito il token di rinnovo: riprova l'accesso.");
            }

            lock (_lock)
            {
                _cached = tokens;
                _refreshTokens.Save(tokens.RefreshToken);
            }
        }

        /// <exception cref="GoogleAuthException">Nessun accesso valido: serve accedere di nuovo.</exception>
        public string GetAccessToken()
        {
            lock (_lock)
            {
                if (_cached != null && _cached.ExpiresAtUtc > _utcNow().AddMinutes(1))
                {
                    return _cached.AccessToken;
                }

                var refresh = _refreshTokens.Load();
                if (refresh == null)
                {
                    throw new GoogleAuthException("Non hai ancora fatto l'accesso a Google.");
                }

                GoogleTokens fresh;
                try
                {
                    fresh = _oauth.RefreshAsync(refresh).GetAwaiter().GetResult();
                }
                catch (GoogleAuthException)
                {
                    _cached = null;
                    throw;
                }

                if (!string.IsNullOrEmpty(fresh.RefreshToken) && fresh.RefreshToken != refresh)
                {
                    _refreshTokens.Save(fresh.RefreshToken);
                }

                _cached = fresh;
                return fresh.AccessToken;
            }
        }

        /// <summary>Scarta il token in memoria (per esempio dopo un 401): il prossimo uso lo rinnova.</summary>
        public void Invalidate()
        {
            lock (_lock)
            {
                _cached = null;
            }
        }

        public void SignOut()
        {
            lock (_lock)
            {
                _cached = null;
                _refreshTokens.Delete();
            }
        }
    }
}
