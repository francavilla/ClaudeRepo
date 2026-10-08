using System;

namespace PasswordGen.Core.Security
{
    /// <summary>PIN o password dell'app: impostazione, verifica con attesa crescente e rimozione, con salvataggio a ogni cambiamento.</summary>
    public sealed class LockCredentialManager
    {
        private readonly LockCredentialStore _store;
        private readonly Func<DateTime> _utcNow;
        private LockCredential _credential;

        public LockCredentialManager(LockCredentialStore store, Func<DateTime> utcNow = null)
        {
            _store = store;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _credential = store.Load();
        }

        /// <summary>True se è stato impostato un PIN o una password.</summary>
        public bool HasCredential
        {
            get { return _credential != null; }
        }

        public CredentialKind? Kind
        {
            get { return _credential == null ? (CredentialKind?)null : _credential.Kind; }
        }

        /// <summary>Imposta (o sostituisce) il PIN o la password; azzera i tentativi sbagliati.</summary>
        public void Set(CredentialKind kind, string secret, int iterations = LockCredential.DefaultIterations)
        {
            var credential = LockCredential.Create(kind, secret, iterations);
            _store.Save(credential);
            _credential = credential;
        }

        public void Clear()
        {
            _store.Delete();
            _credential = null;
        }

        /// <summary>Quanto manca prima di poter riprovare (zero se si può provare subito).</summary>
        public TimeSpan RetryAfter()
        {
            return _credential == null ? TimeSpan.Zero : _credential.RemainingLock(_utcNow());
        }

        public CredentialCheckResult Check(string secret)
        {
            if (_credential == null)
            {
                return new CredentialCheckResult(CredentialCheck.Wrong, TimeSpan.Zero, 0);
            }

            var result = _credential.Check(secret, _utcNow());
            _store.Save(_credential);
            return result;
        }
    }
}
