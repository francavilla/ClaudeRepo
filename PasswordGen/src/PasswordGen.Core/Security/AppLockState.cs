using System;

namespace PasswordGen.Core.Security
{
    /// <summary>
    /// Stato del blocco dell'app: quando va richiesta l'autenticazione (impronta, PIN, Windows Hello).
    /// L'app resta bloccata all'avvio e dopo essere rimasta in secondo piano per più del tempo di tolleranza.
    /// La logica non dipende dalla piattaforma; l'autenticazione vera è fatta da Android o da Windows.
    /// </summary>
    public sealed class AppLockState
    {
        public const int MaxGraceSeconds = 3600;

        private DateTime? _leftAt;
        private bool _enabled;

        public AppLockState(TimeSpan gracePeriod)
        {
            GracePeriod = gracePeriod < TimeSpan.Zero ? TimeSpan.Zero : gracePeriod;
        }

        /// <summary>Quanto si può restare in secondo piano senza dover ripetere l'autenticazione (zero = sempre).</summary>
        public TimeSpan GracePeriod { get; set; }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                if (!value)
                {
                    IsLocked = false;
                    _leftAt = null;
                }
            }
        }

        /// <summary>True se l'app deve restare nascosta finché l'utente non si autentica.</summary>
        public bool IsLocked { get; private set; }

        /// <summary>All'avvio dell'app: se il blocco è attivo, si parte bloccati.</summary>
        public void Start()
        {
            IsLocked = _enabled;
            _leftAt = null;
        }

        /// <summary>L'app va in secondo piano.</summary>
        public void Backgrounded(DateTime now)
        {
            if (_leftAt == null)
            {
                _leftAt = now;
            }
        }

        /// <summary>L'app torna in primo piano: restituisce true se ora è bloccata.</summary>
        public bool Foregrounded(DateTime now)
        {
            if (_enabled && _leftAt.HasValue && now - _leftAt.Value >= GracePeriod)
            {
                IsLocked = true;
            }

            _leftAt = null;
            return IsLocked;
        }

        /// <summary>Blocca subito (pulsante «Blocca adesso»); non ha effetto se il blocco è disattivato.</summary>
        public void Lock()
        {
            if (_enabled)
            {
                IsLocked = true;
            }
        }

        /// <summary>L'utente si è autenticato.</summary>
        public void Unlocked()
        {
            IsLocked = false;
            _leftAt = null;
        }
    }
}
