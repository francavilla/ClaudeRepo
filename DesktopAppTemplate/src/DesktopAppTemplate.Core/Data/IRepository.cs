using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopAppTemplate.Core.Data
{
    /// <summary>
    /// Operazioni di base di un archivio di entità. Ogni slice deriva la propria interfaccia
    /// (es. <c>ITaskRepository : IRepository&lt;TaskItem, Guid&gt;</c>) e aggiunge solo le query che le servono.
    /// </summary>
    public interface IRepository<TEntity, in TId>
    {
        Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken);
        Task<TEntity> GetAsync(TId id, CancellationToken cancellationToken);
        Task AddAsync(TEntity entity, CancellationToken cancellationToken);
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken);
        Task RemoveAsync(TId id, CancellationToken cancellationToken);
    }
}
