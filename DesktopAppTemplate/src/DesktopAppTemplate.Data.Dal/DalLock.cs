using System;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopAppTemplate.Data.Dal
{
    /// <summary>
    /// Serializza l'uso della connessione singleton di DAL: una sola operazione per volta, senza bloccare l'interfaccia
    /// (attesa asincrona, chiamate DAL eseguite fuori dal thread della UI).
    /// Durante una transazione il blocco è tenuto per tutta la durata: altre operazioni attendono (altrimenti, sulla
    /// connessione condivisa, finirebbero dentro la transazione), mentre quelle fatte dal codice della transazione stessa
    /// passano senza riacquistarlo (<see cref="AsyncLocal{T}"/>).
    /// </summary>
    public sealed class DalLock
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly AsyncLocal<bool> _insideExclusive = new AsyncLocal<bool>();

        /// <summary>True se il codice in esecuzione sta già dentro un blocco esclusivo (es. una transazione).</summary>
        public bool IsInsideExclusive => _insideExclusive.Value;

        /// <summary>Esegue un'operazione sulla connessione, da sola.</summary>
        public async Task<T> RunAsync<T>(Func<T> work, CancellationToken cancellationToken)
        {
            if (_insideExclusive.Value)
                return await Task.Run(work, cancellationToken).ConfigureAwait(false);

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await Task.Run(work, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>Tiene il blocco per tutta la durata di <paramref name="body"/> (usato dalle transazioni).</summary>
        public async Task RunExclusiveAsync(Func<Task> body, CancellationToken cancellationToken)
        {
            if (_insideExclusive.Value)
            {
                await body().ConfigureAwait(false);
                return;
            }

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _insideExclusive.Value = true;
                await body().ConfigureAwait(false);
            }
            finally
            {
                _insideExclusive.Value = false;
                _semaphore.Release();
            }
        }
    }
}
