using System;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Data;

namespace DesktopAppTemplate.Data.Dal
{
    /// <summary>
    /// Transazione esplicita su DAL: apre la transazione, esegue il lavoro, conferma; se il lavoro solleva un'eccezione
    /// annulla e la rilancia. Le transazioni di DAL non sono implicite: senza questo ogni comando è confermato subito.
    /// Una transazione richiesta dentro un'altra non ne apre una seconda (la connessione è una sola): partecipa a quella
    /// esterna, che conferma o annulla tutto insieme.
    /// </summary>
    public sealed class DalTransactionRunner : ITransactionRunner
    {
        private readonly IDalGateway _gateway;
        private readonly DalLock _lock;

        public DalTransactionRunner(IDalGateway gateway, DalLock dalLock)
        {
            _gateway = gateway;
            _lock = dalLock;
        }

        public Task RunAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_lock.IsInsideExclusive)
                return work(cancellationToken);

            return _lock.RunExclusiveAsync(async () =>
            {
                await Task.Run(() => _gateway.BeginTransaction(), cancellationToken).ConfigureAwait(false);
                try
                {
                    await work(cancellationToken).ConfigureAwait(false);
                    await Task.Run(() => _gateway.Commit(), CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    try
                    {
                        await Task.Run(() => _gateway.Rollback(), CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Se anche l'annullamento fallisce, l'errore importante è quello originale, che viene rilanciato sotto.
                    }

                    throw;
                }
            }, cancellationToken);
        }
    }
}
