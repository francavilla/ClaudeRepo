using System;
using DesktopAppTemplate.Core.Data;

namespace DesktopAppTemplate.Features.Tasks
{
    /// <summary>
    /// Archivio delle attività. Definito dalla slice, implementato fuori (file JSON, ADO.NET, Dapper, Entity Framework):
    /// la slice conosce solo questa interfaccia (inversione delle dipendenze). Le operazioni di base vengono da
    /// <see cref="IRepository{TEntity,TId}"/>; qui si aggiungerebbero le sole query specifiche delle attività.
    /// </summary>
    public interface ITaskRepository : IRepository<TaskItem, Guid>
    {
    }
}
