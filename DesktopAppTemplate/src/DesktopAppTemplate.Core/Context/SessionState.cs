using System;
using System.Collections.Generic;

namespace DesktopAppTemplate.Core.Context
{
    /// <summary>Implementazione thread-safe di <see cref="ISessionState"/>.</summary>
    public sealed class SessionState : ISessionState
    {
        private readonly object _gate = new object();
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler<SessionChangedEventArgs> Changed;

        public T Get<T>(string key, T defaultValue = default(T))
        {
            lock (_gate)
            {
                object value;
                return _values.TryGetValue(key, out value) && value is T ? (T)value : defaultValue;
            }
        }

        public void Set<T>(string key, T value)
        {
            lock (_gate)
            {
                object current;
                if (_values.TryGetValue(key, out current) && Equals(current, value))
                    return;

                _values[key] = value;
            }

            Changed?.Invoke(this, new SessionChangedEventArgs(key));
        }

        public bool Remove(string key)
        {
            bool removed;
            lock (_gate)
            {
                removed = _values.Remove(key);
            }

            if (removed)
                Changed?.Invoke(this, new SessionChangedEventArgs(key));
            return removed;
        }
    }
}
