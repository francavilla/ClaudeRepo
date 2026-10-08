using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SQLite;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Data;
using DesktopAppTemplate.Data.Tasks;
using DesktopAppTemplate.Features.Tasks;

namespace DesktopAppTemplate.Data.EntityFramework
{
    /// <summary>
    /// Archivio delle attività con Entity Framework 6. Un contesto (e una connessione) per operazione.
    /// EF6 richiede le connessioni dei propri provider: per SQLite crea una <see cref="SQLiteConnection"/>
    /// (System.Data.SQLite) dalla stessa stringa di connessione.
    /// </summary>
    public sealed class EntityFrameworkTaskRepository : ITaskRepository
    {
        private readonly IDbConnectionFactory _connections;

        public EntityFrameworkTaskRepository(IDbConnectionFactory connections)
        {
            _connections = connections;
        }

        public async Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken)
        {
            using (var context = CreateContext())
            {
                var rows = await context.Tasks.AsNoTracking().OrderBy(r => r.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
                return rows.Select(TaskRowMapper.ToItem).ToList();
            }
        }

        public async Task<TaskItem> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            var key = id.ToString("D");
            using (var context = CreateContext())
            {
                var row = await context.Tasks.AsNoTracking().FirstOrDefaultAsync(r => r.Id == key, cancellationToken).ConfigureAwait(false);
                return row == null ? null : TaskRowMapper.ToItem(row);
            }
        }

        public async Task AddAsync(TaskItem item, CancellationToken cancellationToken)
        {
            using (var context = CreateContext())
            {
                context.Tasks.Add(TaskRowMapper.ToRow(item));
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task UpdateAsync(TaskItem item, CancellationToken cancellationToken)
        {
            using (var context = CreateContext())
            {
                context.Entry(TaskRowMapper.ToRow(item)).State = EntityState.Modified;
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task RemoveAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var context = CreateContext())
            {
                context.Entry(new TaskRow { Id = id.ToString("D") }).State = EntityState.Deleted;
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private TaskDbContext CreateContext()
        {
            DbConnection connection;
            switch (_connections.Provider)
            {
                case DatabaseProvider.Sqlite:
                    connection = new SQLiteConnection(_connections.ConnectionString);
                    break;
                case DatabaseProvider.SqlServer:
                    connection = new SqlConnection(_connections.ConnectionString);
                    break;
                default:
                    throw new NotSupportedException("Database non supportato: " + _connections.Provider);
            }

            return new TaskDbContext(connection);
        }
    }
}
