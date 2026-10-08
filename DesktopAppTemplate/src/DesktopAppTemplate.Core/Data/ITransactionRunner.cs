using System;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopAppTemplate.Core.Data
{
    /// <summary>
    /// Esegue un gruppo di operazioni in una transazione esplicita: conferma se il lavoro termina, annulla se solleva un'eccezione.
    /// Le operazioni eseguite dentro <c>work</c> (tramite i repository) fanno parte della transazione.
    /// </summary>
    public interface ITransactionRunner
    {
        Task RunAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default(CancellationToken));
    }
}
