using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PasswordGen.Core.Sync.Google
{
    /// <summary>
    /// Riceve il ritorno di Google dopo l'accesso nel browser (app per computer): un piccolo server su 127.0.0.1, su una porta libera,
    /// che accetta una sola richiesta, ne legge i parametri (code, state, error) e mostra una pagina «puoi chiudere questa finestra».
    /// Non è raggiungibile da altri computer. Si usa un socket semplice per non richiedere i permessi di amministratore di HttpListener.
    /// </summary>
    public sealed class LoopbackReceiver : IDisposable
    {
        private readonly TcpListener _listener;

        public LoopbackReceiver()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        }

        public int Port { get; private set; }

        /// <summary>L'indirizzo di ritorno da dichiarare a Google.</summary>
        public string RedirectUri
        {
            get { return "http://127.0.0.1:" + Port + "/"; }
        }

        /// <summary>Attende il ritorno del browser e restituisce i parametri della richiesta. Null se scade il tempo o si annulla.</summary>
        public async Task<Dictionary<string, string>> WaitAsync(TimeSpan timeout, CancellationToken cancel)
        {
            using (var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancel))
            {
                timeoutSource.CancelAfter(timeout);
                using (timeoutSource.Token.Register(() => _listener.Stop()))
                {
                    try
                    {
                        // Il browser può aprire connessioni di prova (precaricamento): si risponde a ognuna finché arriva quella con i parametri.
                        while (true)
                        {
                            using (var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false))
                            {
                                var parameters = await ReadAndAnswerAsync(client).ConfigureAwait(false);
                                if (parameters != null && (parameters.ContainsKey("code") || parameters.ContainsKey("error")))
                                {
                                    return parameters;
                                }
                            }
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        return null;
                    }
                    catch (SocketException)
                    {
                        return null;
                    }
                    catch (InvalidOperationException)
                    {
                        return null;
                    }
                }
            }
        }

        private static async Task<Dictionary<string, string>> ReadAndAnswerAsync(TcpClient client)
        {
            var stream = client.GetStream();
            var buffer = new byte[8192];
            var read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            var request = Encoding.ASCII.GetString(buffer, 0, read);

            // Prima riga: «GET /?code=...&state=... HTTP/1.1»
            var firstLine = request.Split('\n')[0].Trim();
            var parts = firstLine.Split(' ');
            Dictionary<string, string> parameters = null;
            if (parts.Length >= 2 && parts[0] == "GET")
            {
                var target = parts[1];
                var queryStart = target.IndexOf('?');
                parameters = queryStart >= 0 ? ParseQuery(target.Substring(queryStart + 1)) : new Dictionary<string, string>();
            }

            var ok = parameters != null && parameters.ContainsKey("code");
            var body = "<!doctype html><html lang=\"it\"><head><meta charset=\"utf-8\"><title>PasswordGen</title></head>"
                + "<body style=\"font-family:Segoe UI,Arial,sans-serif;text-align:center;margin-top:15vh\">"
                + (ok
                    ? "<h2>Accesso completato</h2><p>Puoi chiudere questa finestra e tornare a PasswordGen.</p>"
                    : "<h2>Accesso non completato</h2><p>Puoi chiudere questa finestra e riprovare da PasswordGen.</p>")
                + "</body></html>";
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var header = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: " + bodyBytes.Length + "\r\nConnection: close\r\n\r\n";
            var headerBytes = Encoding.ASCII.GetBytes(header);
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length).ConfigureAwait(false);
            await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
            return parameters;
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in query.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var index = pair.IndexOf('=');
                var name = index < 0 ? pair : pair.Substring(0, index);
                var value = index < 0 ? string.Empty : pair.Substring(index + 1);
                result[Uri.UnescapeDataString(name)] = Uri.UnescapeDataString(value.Replace('+', ' '));
            }

            return result;
        }

        public void Dispose()
        {
            try
            {
                _listener.Stop();
            }
            catch (Exception)
            {
                // Già fermato.
            }
        }
    }
}
