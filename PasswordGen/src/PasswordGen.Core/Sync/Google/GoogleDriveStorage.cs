using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace PasswordGen.Core.Sync.Google
{
    [DataContract]
    internal sealed class DriveFileList
    {
        [DataMember(Name = "files")] public List<DriveFile> Files { get; set; }
    }

    [DataContract]
    internal sealed class DriveErrorDetail
    {
        [DataMember(Name = "reason")] public string Reason { get; set; }
    }

    [DataContract]
    internal sealed class DriveErrorBody
    {
        [DataMember(Name = "message")] public string Message { get; set; }
        [DataMember(Name = "errors")] public List<DriveErrorDetail> Errors { get; set; }
    }

    [DataContract]
    internal sealed class DriveErrorResponse
    {
        [DataMember(Name = "error")] public DriveErrorBody Error { get; set; }
    }

    [DataContract]
    internal sealed class DriveFile
    {
        [DataMember(Name = "id")] public string Id { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
    }

    /// <summary>
    /// File di sincronizzazione nel Google Drive dell'utente (cartella principale «Il mio Drive»). Con l'ambito drive.file l'app vede solo
    /// i file che ha creato lei: il file lo crea l'app, e può poi essere letto anche dal Drive del computer come un normale file.
    /// Le chiamate sono sincrone: vanno eseguite fuori dal thread dell'interfaccia (lo fa <see cref="SyncEngine.RunAsync"/>).
    /// </summary>
    public sealed class GoogleDriveStorage : ISyncStorage
    {
        public const string DefaultFileName = "PasswordGen-sync.pgx";

        private const string FilesUrl = "https://www.googleapis.com/drive/v3/files";
        private const string UploadUrl = "https://www.googleapis.com/upload/drive/v3/files";

        private readonly HttpClient _http;
        private readonly GoogleAccessTokenProvider _tokens;
        private readonly string _fileName;

        public GoogleDriveStorage(HttpClient http, GoogleAccessTokenProvider tokens, string fileName = DefaultFileName)
        {
            _http = http;
            _tokens = tokens;
            _fileName = fileName;
        }

        public byte[] Read()
        {
            var id = FindFileId();
            if (id == null)
            {
                return null;
            }

            using (var response = Send(() => new HttpRequestMessage(HttpMethod.Get, FilesUrl + "/" + Uri.EscapeDataString(id) + "?alt=media")))
            {
                EnsureSuccess(response, "lettura del file");
                return response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            }
        }

        public void Write(byte[] data)
        {
            var id = FindFileId();
            if (id != null)
            {
                using (var response = Send(() => new HttpRequestMessage(new HttpMethod("PATCH"), UploadUrl + "/" + Uri.EscapeDataString(id) + "?uploadType=media")
                {
                    Content = Binary(data)
                }))
                {
                    EnsureSuccess(response, "aggiornamento del file");
                }

                return;
            }

            using (var response = Send(() =>
            {
                var multipart = new MultipartContent("related", "pgx" + Guid.NewGuid().ToString("N"));
                multipart.Add(new StringContent("{\"name\":\"" + _fileName + "\"}", Encoding.UTF8, "application/json"));
                multipart.Add(Binary(data));
                return new HttpRequestMessage(HttpMethod.Post, UploadUrl + "?uploadType=multipart&fields=id") { Content = multipart };
            }))
            {
                EnsureSuccess(response, "creazione del file");
            }
        }

        private string FindFileId()
        {
            var query = "name='" + _fileName.Replace("'", "\\'") + "' and trashed=false";
            var url = FilesUrl + "?q=" + Uri.EscapeDataString(query) + "&fields=files(id,name)&pageSize=1";
            using (var response = Send(() => new HttpRequestMessage(HttpMethod.Get, url)))
            {
                EnsureSuccess(response, "ricerca del file");
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(body)))
                {
                    var list = (DriveFileList)new DataContractJsonSerializer(typeof(DriveFileList)).ReadObject(stream);
                    return list != null && list.Files != null && list.Files.Count > 0 ? list.Files[0].Id : null;
                }
            }
        }

        private static ByteArrayContent Binary(byte[] data)
        {
            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return content;
        }

        /// <summary>Invia la richiesta con il token; se Google risponde 401 rinnova il token e riprova una volta.</summary>
        private HttpResponseMessage Send(Func<HttpRequestMessage> build)
        {
            for (var attempt = 0; ; attempt++)
            {
                var request = build();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.GetAccessToken());
                var response = _http.SendAsync(request).GetAwaiter().GetResult();
                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                {
                    response.Dispose();
                    _tokens.Invalidate();
                    continue;
                }

                return response;
            }
        }

        private static void EnsureSuccess(HttpResponseMessage response, string what)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var detail = ReadGoogleError(response);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new GoogleAuthException("Google ha rifiutato l'accesso: accedi di nuovo." + detail);
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new IOException("Google Drive non permette l'operazione (" + what + "): controlla di aver consentito l'accesso ai file di Drive e che l'API di Drive sia abilitata nel progetto." + detail);
            }

            throw new IOException("Google Drive: errore nella " + what + " (HTTP " + (int)response.StatusCode + ")." + detail);
        }

        /// <summary>Il motivo scritto da Google nella risposta d'errore (JSON), per capire cosa non va; vuoto se non c'è.</summary>
        private static string ReadGoogleError(HttpResponseMessage response)
        {
            try
            {
                var body = response.Content == null ? null : response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(body))
                {
                    return string.Empty;
                }

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(body)))
                {
                    var parsed = (DriveErrorResponse)new DataContractJsonSerializer(typeof(DriveErrorResponse)).ReadObject(stream);
                    if (parsed == null || parsed.Error == null)
                    {
                        return string.Empty;
                    }

                    var reason = parsed.Error.Errors != null && parsed.Error.Errors.Count > 0 ? parsed.Error.Errors[0].Reason : null;
                    var text = parsed.Error.Message;
                    if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(reason))
                    {
                        return string.Empty;
                    }

                    return " Google: " + text + (string.IsNullOrEmpty(reason) ? string.Empty : " [" + reason + "]");
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
