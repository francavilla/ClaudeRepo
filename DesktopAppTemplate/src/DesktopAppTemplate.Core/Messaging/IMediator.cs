using System.Threading;
using System.Threading.Tasks;

namespace DesktopAppTemplate.Core.Messaging
{
    /// <summary>Punto unico con cui i view model invocano i casi d'uso, senza conoscere gli handler.</summary>
    public interface IMediator
    {
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default(CancellationToken));
    }
}
