using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Data;

namespace DesktopAppTemplate.Data
{
    /// <summary>
    /// Executor ADO.NET predefinito: una connessione per operazione, parametri nominati, dialetto e opzioni sostituibili.
    /// Per usare una libreria esistente si può sostituire solo questa classe (o solo la fabbrica di connessioni).
    /// </summary>
    public sealed class AdoNetExecutor : IDbExecutor
    {
        private readonly IDbConnectionFactory _connections;
        private readonly ISqlDialect _dialect;
        private readonly AdoNetOptions _options;

        public AdoNetExecutor(IDbConnectionFactory connections, ISqlDialect dialect, AdoNetOptions options = null)
        {
            _connections = connections ?? throw new ArgumentNullException(nameof(connections));
            _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
            _options = options ?? new AdoNetOptions();
        }

        public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, IDictionary<string, object> parameters,
            Func<IDataRecord, T> map, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var connection = await _connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            using (var command = CreateCommand(connection, sql, parameters))
            using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                var rows = new List<T>();
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    rows.Add(map(reader));
                return rows;
            }
        }

        public async Task<int> ExecuteAsync(string sql, IDictionary<string, object> parameters = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var connection = await _connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            using (var command = CreateCommand(connection, sql, parameters))
            {
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private DbCommand CreateCommand(DbConnection connection, string sql, IDictionary<string, object> parameters)
        {
            var command = connection.CreateCommand();
            command.CommandText = _dialect.Adapt(sql);
            command.CommandTimeout = _options.CommandTimeoutSeconds;

            if (parameters != null)
            {
                foreach (var pair in parameters)
                    command.AddParameter(_dialect.ParameterPrefix + pair.Key, pair.Value);
            }

            _options.ConfigureCommand?.Invoke(command);
            return command;
        }
    }
}
