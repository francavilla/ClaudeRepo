using System.Threading;
using System.Threading.Tasks;

namespace Catalog.Application.Abstractions
{
    /// <summary>Command che non restituisce un valore.</summary>
    public interface ICommandHandler<in TCommand>
    {
        Task HandleAsync(TCommand command, CancellationToken cancellationToken);
    }

    /// <summary>Command che restituisce un risultato (es. l'Id della risorsa creata).</summary>
    public interface ICommandHandler<in TCommand, TResult>
    {
        Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
    }
}
