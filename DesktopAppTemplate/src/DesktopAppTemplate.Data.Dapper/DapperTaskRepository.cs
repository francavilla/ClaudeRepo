using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DesktopAppTemplate.Core.Data;
using DesktopAppTemplate.Data.Tasks;
using DesktopAppTemplate.Features.Tasks;

namespace DesktopAppTemplate.Data.Dapper
{
    /// <summary>Archivio delle attività con Dapper: stesso SQL di <c>TaskSql</c>, righe lette direttamente in <see cref="TaskRow"/>.</summary>
    public sealed class DapperTaskRepository : ITaskRepository
    {
        private readonly IDbConnectionFactory _connections;
        private readonly ISqlDialect _dialect;

        public DapperTaskRepository(IDbConnectionFactory connections, ISqlDialect dialect)
        {
            _connections = connections;
            _dialect = dialect;
        }

        public async Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken)
        {
            using (var connection = await _connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                var rows = await connection.QueryAsync<TaskRow>(Command(TaskSql.SelectAll, null, cancellationToken)).ConfigureAwait(false);
                return rows.Select(TaskRowMapper.ToItem).ToList();
            }
        }

        public async Task<TaskItem> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var connection = await _connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                var row = await connection.QueryFirstOrDefaultAsync<TaskRow>(
                    Command(TaskSql.SelectById, new { Id = id.ToString("D") }, cancellationToken)).ConfigureAwait(false);
                return row == null ? null : TaskRowMapper.ToItem(row);
            }
        }

        public Task AddAsync(TaskItem item, CancellationToken cancellationToken)
        {
            return ExecuteAsync(TaskSql.Insert, TaskRowMapper.ToRow(item), cancellationToken);
        }

        public Task UpdateAsync(TaskItem item, CancellationToken cancellationToken)
        {
            return ExecuteAsync(TaskSql.Update, TaskRowMapper.ToRow(item), cancellationToken);
        }

        public Task RemoveAsync(Guid id, CancellationToken cancellationToken)
        {
            return ExecuteAsync(TaskSql.Delete, new { Id = id.ToString("D") }, cancellationToken);
        }

        private async Task ExecuteAsync(string sql, object parameters, CancellationToken cancellationToken)
        {
            using (var connection = await _connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                await connection.ExecuteAsync(Command(sql, parameters, cancellationToken)).ConfigureAwait(false);
            }
        }

        private CommandDefinition Command(string sql, object parameters, CancellationToken cancellationToken)
        {
            return new CommandDefinition(_dialect.Adapt(sql), parameters, cancellationToken: cancellationToken);
        }
    }
}
