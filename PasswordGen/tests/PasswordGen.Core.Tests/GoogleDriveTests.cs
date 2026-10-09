using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PasswordGen.Core.Generation;
using PasswordGen.Core.History;
using PasswordGen.Core.Settings;
using PasswordGen.Core.Sync;
using PasswordGen.Core.Sync.Google;
using Xunit;

namespace PasswordGen.Core.Tests
{
    public class GoogleDriveTests : IDisposable
    {
        private const string ClientId = "123-abc.apps.googleusercontent.com";
        private const string Redirect = "com.googleusercontent.apps.123-abc:/oauth2redirect";
        private static readonly DateTime Now = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "PasswordGenGoogle_" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        private sealed class XorProtector : ISecretProtector
        {
            public byte[] Protect(byte[] data) { return data.Select(b => (byte)(b ^ 0x5A)).ToArray(); }
            public byte[] Unprotect(byte[] data) { return data.Select(b => (byte)(b ^ 0x5A)).ToArray(); }
        }

        /// <summary>Google finto: token endpoint e un Drive in memoria con un solo file (quello che crea l'app).</summary>
        private sealed class FakeGoogle : HttpMessageHandler
        {
            public string AccessToken = "token-1";
            public string RefreshTokenReturned;        // se valorizzato, il rinnovo restituisce anche un nuovo token di rinnovo
            public string TokenError;                   // se valorizzato, il token endpoint risponde con questo errore (400)
            public int TokenCalls;
            public string LastTokenForm;
            public int Counter = 1;
            public byte[] FileContent;
            public string FileId;
            public string FailBody;
            public int FailNextWithStatus;              // se > 0, la prossima chiamata a Drive risponde con questo codice
            public bool RejectFirstDriveCall;           // la prima chiamata a Drive riceve 401 (token scaduto)
            public readonly List<string> Calls = new List<string>();
            public readonly List<string> Tokens = new List<string>();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var url = request.RequestUri.ToString();
                Calls.Add(request.Method + " " + request.RequestUri.GetLeftPart(UriPartial.Path));

                if (url.StartsWith(GoogleOAuthClient.TokenEndpoint, StringComparison.Ordinal))
                {
                    TokenCalls++;
                    LastTokenForm = await request.Content.ReadAsStringAsync();
                    if (TokenError != null)
                    {
                        return Json(HttpStatusCode.BadRequest, "{\"error\":\"" + TokenError + "\",\"error_description\":\"prova\"}");
                    }

                    AccessToken = "token-" + (++Counter);
                    var refresh = RefreshTokenReturned != null ? ",\"refresh_token\":\"" + RefreshTokenReturned + "\"" : string.Empty;
                    return Json(HttpStatusCode.OK, "{\"access_token\":\"" + AccessToken + "\",\"expires_in\":3600" + refresh + "}");
                }

                Tokens.Add(request.Headers.Authorization == null ? null : request.Headers.Authorization.Parameter);
                if (RejectFirstDriveCall)
                {
                    RejectFirstDriveCall = false;
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }

                if (FailNextWithStatus > 0)
                {
                    var status = (HttpStatusCode)FailNextWithStatus;
                    FailNextWithStatus = 0;
                    var failure = new HttpResponseMessage(status);
                    if (FailBody != null)
                    {
                        failure.Content = new StringContent(FailBody, Encoding.UTF8, "application/json");
                    }

                    return failure;
                }

                if (request.Method == HttpMethod.Get && url.Contains("alt=media"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(FileContent) };
                }

                if (request.Method == HttpMethod.Get)
                {
                    return Json(HttpStatusCode.OK, FileId == null ? "{\"files\":[]}" : "{\"files\":[{\"id\":\"" + FileId + "\",\"name\":\"PasswordGen-sync.pgx\"}]}");
                }

                if (request.Method.Method == "PATCH")
                {
                    FileContent = await request.Content.ReadAsByteArrayAsync();
                    return Json(HttpStatusCode.OK, "{\"id\":\"" + FileId + "\"}");
                }

                if (request.Method == HttpMethod.Post)
                {
                    // multipart/related: seconda parte = contenuto del file (lettura byte per byte con Latin1, che li conserva).
                    var raw = Encoding.GetEncoding("ISO-8859-1").GetString(await request.Content.ReadAsByteArrayAsync());
                    var boundary = request.Content.Headers.ContentType.Parameters.First(p => p.Name == "boundary").Value.Trim('"');
                    var parts = raw.Split(new[] { "--" + boundary }, StringSplitOptions.None);
                    var content = parts[2];
                    content = content.Substring(content.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4);
                    content = content.Substring(0, content.LastIndexOf("\r\n", StringComparison.Ordinal));
                    FileContent = Encoding.GetEncoding("ISO-8859-1").GetBytes(content);
                    FileId = "file-1";
                    return Json(HttpStatusCode.OK, "{\"id\":\"file-1\"}");
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            private static HttpResponseMessage Json(HttpStatusCode status, string body)
            {
                return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            }
        }

        private sealed class Rig
        {
            public FakeGoogle Google;
            public GoogleOAuthClient OAuth;
            public GoogleAccessTokenProvider Tokens;
            public SyncPassphraseStore RefreshStore;
            public GoogleDriveStorage Storage;
            public DateTime Clock;
        }

        private Rig CreateRig(bool signedIn = true)
        {
            var rig = new Rig { Google = new FakeGoogle(), Clock = Now };
            var http = new HttpClient(rig.Google);
            rig.OAuth = new GoogleOAuthClient(http, ClientId, Redirect, () => rig.Clock);
            rig.RefreshStore = new SyncPassphraseStore(Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".key"), new XorProtector());
            rig.Tokens = new GoogleAccessTokenProvider(rig.OAuth, rig.RefreshStore, () => rig.Clock);
            if (signedIn)
            {
                rig.Tokens.SignIn(new GoogleTokens { AccessToken = "token-1", RefreshToken = "refresh-1", ExpiresAtUtc = Now.AddHours(1) });
            }

            rig.Storage = new GoogleDriveStorage(http, rig.Tokens);
            return rig;
        }

        // ---- accesso (OAuth) ----

        [Fact]
        public void Pkce_SfidaEVerificatoreCoerenti()
        {
            var pkce = GoogleOAuthClient.CreatePkce();

            Assert.InRange(pkce.Verifier.Length, 43, 128);
            using (var sha = SHA256.Create())
            {
                var expected = Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(pkce.Verifier))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                Assert.Equal(expected, pkce.Challenge);
            }

            Assert.NotEqual(pkce.Verifier, GoogleOAuthClient.CreatePkce().Verifier);
        }

        [Fact]
        public void IndirizzoDiAccesso_ContieneIParametriAttesi()
        {
            var rig = CreateRig(false);
            var pkce = GoogleOAuthClient.CreatePkce();

            var uri = rig.OAuth.BuildAuthorizationUri(pkce).ToString();

            Assert.StartsWith(GoogleOAuthClient.AuthEndpoint, uri);
            Assert.Contains("client_id=" + Uri.EscapeDataString(ClientId), uri);
            Assert.Contains("redirect_uri=" + Uri.EscapeDataString(Redirect), uri);
            Assert.Contains("scope=" + Uri.EscapeDataString(GoogleOAuthClient.DriveFileScope), uri);
            Assert.Contains("code_challenge=" + pkce.Challenge, uri);
            Assert.Contains("code_challenge_method=S256", uri);
            Assert.Contains("state=" + pkce.State, uri);
            Assert.Contains("access_type=offline", uri);
        }

        [Fact]
        public void ScambioDelCodice_RestituisceITokenConScadenza()
        {
            var rig = CreateRig(false);
            rig.Google.RefreshTokenReturned = "refresh-nuovo";

            var tokens = rig.OAuth.ExchangeCodeAsync("codice", "verificatore").GetAwaiter().GetResult();

            Assert.Equal("refresh-nuovo", tokens.RefreshToken);
            Assert.Equal(Now.AddHours(1), tokens.ExpiresAtUtc);
            Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
        }

        [Fact]
        public void Rinnovo_AccessoRevocato_ChiedeUnNuovoAccesso()
        {
            var rig = CreateRig();
            rig.Google.TokenError = "invalid_grant";

            Assert.Throws<GoogleAuthException>(() => rig.OAuth.RefreshAsync("refresh-1").GetAwaiter().GetResult());
        }

        [Fact]
        public void Rinnovo_AltroErrore_EUnErroreDiRete()
        {
            var rig = CreateRig();
            rig.Google.TokenError = "server_error";

            Assert.Throws<IOException>(() => rig.OAuth.RefreshAsync("refresh-1").GetAwaiter().GetResult());
        }

        [Fact]
        public void Provider_UsaIlTokenInMemoriaFinoAllaScadenza_PoiRinnova()
        {
            var rig = CreateRig();

            Assert.Equal("token-1", rig.Tokens.GetAccessToken());
            Assert.Equal(0, rig.Google.TokenCalls);

            rig.Clock = Now.AddMinutes(61);   // scaduto
            var renewed = rig.Tokens.GetAccessToken();

            Assert.Equal(1, rig.Google.TokenCalls);
            Assert.NotEqual("token-1", renewed);
        }

        [Fact]
        public void Provider_SalvaIlNuovoTokenDiRinnovoSeGoogleNeDa_Uno()
        {
            var rig = CreateRig();
            rig.Google.RefreshTokenReturned = "refresh-2";
            rig.Clock = Now.AddHours(2);

            rig.Tokens.GetAccessToken();

            Assert.Equal("refresh-2", rig.RefreshStore.Load());
        }

        [Fact]
        public void Provider_SenzaAccesso_Eccezione_ENessunaChiamata()
        {
            var rig = CreateRig(false);

            Assert.False(rig.Tokens.IsSignedIn);
            Assert.Throws<GoogleAuthException>(() => rig.Tokens.GetAccessToken());
            Assert.Equal(0, rig.Google.TokenCalls);
        }

        [Fact]
        public void Provider_SignIn_SenzaTokenDiRinnovo_Rifiuta()
        {
            var rig = CreateRig(false);

            Assert.Throws<GoogleAuthException>(() => rig.Tokens.SignIn(new GoogleTokens { AccessToken = "x", ExpiresAtUtc = Now.AddHours(1) }));
        }

        [Fact]
        public void Provider_Esci_CancellaIlTokenDiRinnovo()
        {
            var rig = CreateRig();

            rig.Tokens.SignOut();

            Assert.False(rig.Tokens.IsSignedIn);
            Assert.Null(rig.RefreshStore.Load());
        }

        [Fact]
        public void ClientDesktop_LaChiaveDelClienteVaNellaRichiesta_QuelloAndroidNo()
        {
            var google = new FakeGoogle();
            var http = new HttpClient(google);

            new GoogleOAuthClient(http, ClientId, Redirect, () => Now, "chiave-del-client").RefreshAsync("r").GetAwaiter().GetResult();
            Assert.Contains("client_secret=chiave-del-client", google.LastTokenForm);

            new GoogleOAuthClient(http, ClientId, Redirect, () => Now).RefreshAsync("r").GetAwaiter().GetResult();
            Assert.DoesNotContain("client_secret", google.LastTokenForm);
        }

        [Fact]
        public void IndirizzoDiRitornoPersonalizzato_SostituisceQuelloPredefinito()
        {
            var google = new FakeGoogle();
            var oauth = new GoogleOAuthClient(new HttpClient(google), ClientId, Redirect, () => Now);
            var pkce = GoogleOAuthClient.CreatePkce();

            var uri = oauth.BuildAuthorizationUri(pkce, "http://127.0.0.1:5555/").ToString();
            oauth.ExchangeCodeAsync("codice", pkce.Verifier, "http://127.0.0.1:5555/").GetAwaiter().GetResult();

            Assert.Contains("redirect_uri=" + Uri.EscapeDataString("http://127.0.0.1:5555/"), uri);
            Assert.Contains("redirect_uri=" + Uri.EscapeDataString("http://127.0.0.1:5555/"), google.LastTokenForm);
        }

        // ---- ritorno del browser (app per computer) ----

        private static string SendRequest(int port, string requestLine)
        {
            using (var client = new System.Net.Sockets.TcpClient("127.0.0.1", port))
            {
                var stream = client.GetStream();
                var bytes = Encoding.ASCII.GetBytes(requestLine + " HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n");
                stream.Write(bytes, 0, bytes.Length);
                var buffer = new byte[4096];
                var total = new StringBuilder();
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    total.Append(Encoding.UTF8.GetString(buffer, 0, read));
                }

                return total.ToString();
            }
        }

        [Fact]
        public void Loopback_RiceveCodiceEStato_ERispondeConUnaPaginaDiConferma()
        {
            using (var receiver = new LoopbackReceiver())
            {
                var waiting = receiver.WaitAsync(TimeSpan.FromSeconds(20), CancellationToken.None);

                var response = SendRequest(receiver.Port, "GET /?code=4%2Fabc&state=xyz");
                var parameters = waiting.GetAwaiter().GetResult();

                Assert.Equal("4/abc", parameters["code"]);
                Assert.Equal("xyz", parameters["state"]);
                Assert.Contains("200 OK", response);
                Assert.Contains("Accesso completato", response);
                Assert.StartsWith("http://127.0.0.1:", receiver.RedirectUri);
            }
        }

        [Fact]
        public void Loopback_IgnoraLeRichiesteSenzaParametri_ComeIlFavicon()
        {
            using (var receiver = new LoopbackReceiver())
            {
                var waiting = receiver.WaitAsync(TimeSpan.FromSeconds(20), CancellationToken.None);

                SendRequest(receiver.Port, "GET /favicon.ico");
                SendRequest(receiver.Port, "GET /?code=ok&state=s");

                Assert.Equal("ok", waiting.GetAwaiter().GetResult()["code"]);
            }
        }

        [Fact]
        public void Loopback_AccessoNegato_RestituisceLErrore()
        {
            using (var receiver = new LoopbackReceiver())
            {
                var waiting = receiver.WaitAsync(TimeSpan.FromSeconds(20), CancellationToken.None);

                var response = SendRequest(receiver.Port, "GET /?error=access_denied&state=s");

                Assert.Equal("access_denied", waiting.GetAwaiter().GetResult()["error"]);
                Assert.Contains("Accesso non completato", response);
            }
        }

        [Fact]
        public void Loopback_ScadutoIlTempo_RestituisceNull()
        {
            using (var receiver = new LoopbackReceiver())
            {
                Assert.Null(receiver.WaitAsync(TimeSpan.FromMilliseconds(300), CancellationToken.None).GetAwaiter().GetResult());
            }
        }

        [Fact]
        public void Loopback_Annullato_RestituisceNull()
        {
            using (var receiver = new LoopbackReceiver())
            using (var cancel = new CancellationTokenSource())
            {
                var waiting = receiver.WaitAsync(TimeSpan.FromSeconds(20), cancel.Token);
                cancel.CancelAfter(200);

                Assert.Null(waiting.GetAwaiter().GetResult());
            }
        }

        // ---- file su Drive ----

        [Fact]
        public void Drive_FileAssente_ReadRestituisceNull()
        {
            var rig = CreateRig();

            Assert.Null(rig.Storage.Read());
        }

        [Fact]
        public void Drive_ScriveCreaIlFile_PoiLoLegge_PoiLoAggiorna()
        {
            var rig = CreateRig();
            var first = new byte[] { 0, 1, 2, 250, 255, 13, 10, 13, 10, 7 };   // include byte binari e ritorni a capo

            rig.Storage.Write(first);
            Assert.Equal(first, rig.Google.FileContent);
            Assert.Equal(first, rig.Storage.Read());

            var second = new byte[] { 9, 8, 7 };
            rig.Storage.Write(second);

            Assert.Equal(second, rig.Storage.Read());
            Assert.Contains(rig.Google.Calls, c => c.StartsWith("PATCH ") && c.Contains("/upload/drive/v3/files/file-1"));
            Assert.Single(rig.Google.Calls, c => c.StartsWith("POST ") && c.Contains("/upload/drive/v3/files"));
        }

        [Fact]
        public void Drive_TokenScaduto_401_RinnovaERiprova()
        {
            var rig = CreateRig();
            rig.Google.RejectFirstDriveCall = true;

            Assert.Null(rig.Storage.Read());

            Assert.Equal(1, rig.Google.TokenCalls);
            Assert.Equal(2, rig.Google.Tokens.Count);
            Assert.NotEqual(rig.Google.Tokens[0], rig.Google.Tokens[1]);
        }

        [Fact]
        public void Drive_ErroreDelServer_EIoException()
        {
            var rig = CreateRig();
            rig.Google.FailNextWithStatus = 500;

            var ex = Assert.Throws<IOException>(() => rig.Storage.Read());
            Assert.Contains("500", ex.Message);
        }

        [Fact]
        public void Drive_ErroreDiGoogle_NelMessaggioCompareIlMotivo()
        {
            var rig = CreateRig();
            rig.Google.FailNextWithStatus = 403;
            rig.Google.FailBody = "{\"error\":{\"code\":403,\"message\":\"Google Drive API has not been used in project 123 before or it is disabled.\",\"errors\":[{\"reason\":\"accessNotConfigured\"}]}}";

            var ex = Assert.Throws<IOException>(() => rig.Storage.Read());

            Assert.Contains("Google Drive API has not been used", ex.Message);
            Assert.Contains("accessNotConfigured", ex.Message);
        }

        [Fact]
        public void Drive_Vietato_SpiegaChePermesso()
        {
            var rig = CreateRig();
            rig.Google.FailNextWithStatus = 403;

            var ex = Assert.Throws<IOException>(() => rig.Storage.Read());
            Assert.Contains("Drive", ex.Message);
        }

        [Fact]
        public void Drive_RichiestaNonAutorizzataDueVolte_ChiedeNuovoAccesso()
        {
            var rig = CreateRig();
            rig.Google.RejectFirstDriveCall = true;
            rig.Google.TokenError = "invalid_grant";   // il rinnovo dopo il 401 viene rifiutato

            Assert.Throws<GoogleAuthException>(() => rig.Storage.Read());
        }

        // ---- sincronizzazione completa su Drive finto ----

        [Fact]
        public void Sincronizzazione_SuDrive_DueDispositiviSiScambianoLoStorico()
        {
            var rig = CreateRig();
            var historyA = new PasswordHistory();
            historyA.Add("Aaaa-1111-bbbb!", GenerationMode.Passphrase, new DateTime(2026, 9, 1));
            var settingsA = new AppSettings();
            Assert.True(SyncEngine.Run(rig.Storage, "frase di prova", historyA, settingsA, Now, 1000).Succeeded);

            var historyB = new PasswordHistory();
            historyB.Add("Cccc-2222-dddd!", GenerationMode.Passphrase, new DateTime(2026, 10, 1));
            var result = SyncEngine.Run(rig.Storage, "frase di prova", historyB, new AppSettings(), Now.AddMinutes(5), 1000);

            Assert.True(result.Succeeded);
            Assert.Equal(1, result.EntriesAdded);
            Assert.Equal(2, historyB.Entries.Count);
            Assert.DoesNotContain("Aaaa-1111", Encoding.GetEncoding("ISO-8859-1").GetString(rig.Google.FileContent));
        }
    }
}
