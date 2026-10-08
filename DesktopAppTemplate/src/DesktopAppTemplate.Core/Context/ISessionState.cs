using System;

namespace DesktopAppTemplate.Core.Context
{
    public sealed class SessionChangedEventArgs : EventArgs
    {
        public SessionChangedEventArgs(string key)
        {
            Key = key;
        }

        public string Key { get; }
    }

    /// <summary>
    /// Stato di lavoro condiviso tra le pagine (es. il cliente o l'ordine su cui si sta lavorando).
    /// Valori tipizzati per chiave; chi è interessato ascolta <see cref="Changed"/>. Vive solo finché l'app è aperta.
    /// </summary>
    public interface ISessionState
    {
        /// <summary>Raggiunge i sottoscrittori ogni volta che un valore cambia o viene rimosso.</summary>
        event EventHandler<SessionChangedEventArgs> Changed;

        /// <summary>Valore della chiave, oppure <paramref name="defaultValue"/> se manca o ha un altro tipo.</summary>
        T Get<T>(string key, T defaultValue = default(T));

        void Set<T>(string key, T value);

        bool Remove(string key);
    }
}
