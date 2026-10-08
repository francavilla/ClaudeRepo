using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace DesktopAppTemplate.Logging
{
    /// <summary>
    /// Base delle destinazioni di log: chi registra un messaggio lo mette solo in coda (nessuna attesa, nessuna eccezione),
    /// un thread in background lo scrive a gruppi. Se la coda è piena i messaggi nuovi si scartano: il log non deve mai
    /// rallentare o bloccare l'applicazione. Alla chiusura (<see cref="Dispose"/>) la coda viene svuotata.
    /// </summary>
    public abstract class BackgroundLoggerProvider : ILoggerProvider
    {
        private const int BatchSize = 100;

        private readonly BlockingCollection<LogEntry> _queue;
        private readonly ManualResetEventSlim _writerReleased;
        private readonly LogLevel _minimumLevel;
        private readonly Thread _thread;
        private int _disposed;

        /// <param name="minimumLevel">Livello minimo registrato.</param>
        /// <param name="startWriting">False per tenere i messaggi in coda finché non si chiama <see cref="ReleaseWriter"/>.</param>
        /// <param name="capacity">Messaggi in coda oltre i quali i nuovi vengono scartati.</param>
        protected BackgroundLoggerProvider(LogLevel minimumLevel, bool startWriting, int capacity = 10000)
        {
            _minimumLevel = minimumLevel;
            _queue = new BlockingCollection<LogEntry>(capacity);
            _writerReleased = new ManualResetEventSlim(startWriting);
            _thread = new Thread(WriteLoop) { IsBackground = true, Name = GetType().Name };
            _thread.Start();
        }

        public ILogger CreateLogger(string categoryName) => new BackgroundLogger(this, categoryName);

        /// <summary>Scrive un gruppo di messaggi (chiamato dal thread in background; le eccezioni sono assorbite).</summary>
        protected abstract void Write(IReadOnlyList<LogEntry> entries);

        /// <summary>Consente al thread di iniziare a scrivere (se creato con <c>startWriting = false</c>).</summary>
        protected void ReleaseWriter() => _writerReleased.Set();

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            _queue.CompleteAdding();
            _writerReleased.Set();

            // Aspetta che la coda si svuoti, ma senza bloccare la chiusura a tempo indeterminato.
            if (_thread.Join(TimeSpan.FromSeconds(5)))
                _queue.Dispose();
        }

        private bool IsEnabled(LogLevel level) => level != LogLevel.None && level >= _minimumLevel;

        private void Enqueue(LogEntry entry)
        {
            try
            {
                _queue.TryAdd(entry);
            }
            catch (InvalidOperationException)
            {
                // Provider già chiuso: il messaggio si perde, senza conseguenze per chi lo ha registrato.
            }
        }

        private void WriteLoop()
        {
            _writerReleased.Wait();

            foreach (var first in _queue.GetConsumingEnumerable())
            {
                var batch = new List<LogEntry> { first };
                LogEntry next;
                while (batch.Count < BatchSize && _queue.TryTake(out next))
                    batch.Add(next);

                try
                {
                    Write(batch);
                }
                catch (Exception)
                {
                    // Il log non può segnalare i propri errori: l'applicazione deve continuare comunque.
                }
            }
        }

        private sealed class BackgroundLogger : ILogger
        {
            private readonly BackgroundLoggerProvider _provider;
            private readonly string _category;

            public BackgroundLogger(BackgroundLoggerProvider provider, string category)
            {
                _provider = provider;
                _category = category;
            }

            public IDisposable BeginScope<TState>(TState state) => NoScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => _provider.IsEnabled(logLevel);

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                    return;

                string message;
                try
                {
                    message = formatter(state, exception);
                }
                catch (Exception)
                {
                    message = "(messaggio non formattabile)";
                }

                if (string.IsNullOrEmpty(message) && exception == null)
                    return;

                _provider.Enqueue(new LogEntry(DateTime.Now, logLevel, _category, message, exception?.ToString()));
            }
        }

        private sealed class NoScope : IDisposable
        {
            public static readonly NoScope Instance = new NoScope();

            public void Dispose()
            {
            }
        }
    }
}
