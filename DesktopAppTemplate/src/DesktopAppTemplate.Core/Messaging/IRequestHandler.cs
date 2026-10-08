using System.Threading;
using System.Threading.Tasks;

namespace DesktopAppTemplate.Core.Messaging
{
    /// <summary>Gestisce un singolo tipo di richiesta: è il "cuore" di una slice.</summary>
    public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }
}
