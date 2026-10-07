using System.Threading;
using System.Threading.Tasks;

namespace Catalog.Application.Abstractions
{
    /// <summary>
    /// Confine transazionale del caso d'uso: un command handler chiama SaveChangesAsync una sola volta.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
