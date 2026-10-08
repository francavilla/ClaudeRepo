using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopAppTemplate.Features.Tasks
{
    /// <summary>Archivio delle attività. Definito dalla slice, implementato nell'Infrastructure (inversione delle dipendenze).</summary>
    public interface ITaskRepository
    {
        Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken);
        Task<TaskItem> GetAsync(Guid id, CancellationToken cancellationToken);
        Task AddAsync(TaskItem item, CancellationToken cancellationToken);
        Task UpdateAsync(TaskItem item, CancellationToken cancellationToken);
        Task RemoveAsync(Guid id, CancellationToken cancellationToken);
    }
}
