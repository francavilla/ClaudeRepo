using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Data;
using DesktopAppTemplate.Data.Tasks;
using DesktopAppTemplate.Features.Tasks;

namespace DesktopAppTemplate.Data.Ado
{
    /// <summary>
    /// Archivio delle attività con ADO.NET. Non apre connessioni né crea comandi: delega tutto a
    /// <see cref="IDbExecutor"/>, che è il punto in cui si aggancia una libreria esistente.
    /// </summary>
    public sealed class AdoNetTaskRepository : ITaskRepository
    {
        private readonly IDbExecutor _executor;

        public AdoNetTaskRepository(IDbExecutor executor)
        {
            _executor = executor;
        }

        public Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken)
        {
            return _executor.QueryAsync(TaskSql.SelectAll, null, TaskRowMapper.FromRecord, cancellationToken);
        }

        public async Task<TaskItem> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            var rows = await _executor.QueryAsync(TaskSql.SelectById, TaskRowMapper.IdParameter(id), TaskRowMapper.FromRecord, cancellationToken)
                .ConfigureAwait(false);
            return rows.FirstOrDefault();
        }

        public Task AddAsync(TaskItem item, CancellationToken cancellationToken)
        {
            return _executor.ExecuteAsync(TaskSql.Insert, TaskRowMapper.ToParameters(TaskRowMapper.ToRow(item)), cancellationToken);
        }

        public Task UpdateAsync(TaskItem item, CancellationToken cancellationToken)
        {
            return _executor.ExecuteAsync(TaskSql.Update, TaskRowMapper.ToParameters(TaskRowMapper.ToRow(item)), cancellationToken);
        }

        public Task RemoveAsync(Guid id, CancellationToken cancellationToken)
        {
            return _executor.ExecuteAsync(TaskSql.Delete, TaskRowMapper.IdParameter(id), cancellationToken);
        }
    }
}
