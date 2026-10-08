using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace PasswordGen.Services
{
    /// <summary>
    /// Copia la password negli appunti chiedendo a Windows di non inserirla nella cronologia né nella sincronizzazione cloud,
    /// e la cancella dopo <see cref="ClearAfter"/> (solo se gli appunti contengono ancora quella password).
    /// </summary>
    public sealed class SecretClipboard : ISecretClipboard
    {
        private const int Attempts = 5;

        private readonly DispatcherTimer _timer;
        private string _pending;

        public SecretClipboard(TimeSpan clearAfter)
        {
            ClearAfter = clearAfter;
            _timer = new DispatcherTimer { Interval = clearAfter };
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                if (TryClear())
                {
                    var handler = Cleared;
                    if (handler != null)
                    {
                        handler(this, EventArgs.Empty);
                    }
                }
            };
        }

        public TimeSpan ClearAfter { get; private set; }

        public event EventHandler Cleared;

        public bool Copy(string text)
        {
            var data = new DataObject();
            data.SetText(text);
            MarkAsSensitive(data);

            for (var attempt = 0; attempt < Attempts; attempt++)
            {
                try
                {
                    Clipboard.SetDataObject(data, true);
                    _pending = text;
                    _timer.Stop();
                    _timer.Start();
                    return true;
                }
                catch (COMException)
                {
                    // Gli appunti sono occupati da un altro programma: breve attesa e nuovo tentativo.
                    Thread.Sleep(50);
                }
            }

            return false;
        }

        public void ClearIfPending()
        {
            TryClear();
        }

        private bool TryClear()
        {
            var pending = _pending;
            _pending = null;
            _timer.Stop();
            if (pending == null)
            {
                return false;
            }

            try
            {
                if (Clipboard.ContainsText() && Clipboard.GetText() == pending)
                {
                    Clipboard.Clear();
                    return true;
                }
            }
            catch (COMException)
            {
                // Appunti non accessibili in questo momento: non si può fare altro.
            }

            return false;
        }

        /// <summary>Formati riconosciuti da Windows (cronologia Win+V e appunti cloud) e dai gestori di appunti.</summary>
        private static void MarkAsSensitive(DataObject data)
        {
            try
            {
                data.SetData("ExcludeClipboardContentFromMonitorProcessing", new MemoryStream(BitConverter.GetBytes(1)));
                data.SetData("CanIncludeInClipboardHistory", new MemoryStream(BitConverter.GetBytes(0)));
                data.SetData("CanUploadToCloudClipboard", new MemoryStream(BitConverter.GetBytes(0)));
            }
            catch (Exception)
            {
                // Funzione di sicurezza aggiuntiva: se non è disponibile la copia avviene comunque.
            }
        }
    }
}
