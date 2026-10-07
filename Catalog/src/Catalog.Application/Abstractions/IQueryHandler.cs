using System.Threading;
using System.Threading.Tasks;

namespace Catalog.Application.Abstractions
{
    public interface IQueryHandler<in TQuery, TResult>
    {
        Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
    }
}
