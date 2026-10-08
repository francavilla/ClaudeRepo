using System;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Data;
using Microsoft.Data.Sqlite;
using System.Data.SqlClient;

namespace DesktopAppTemplate.Data
{
    /// <summary>Crea connessioni SQLite o SQL Server dalla stringa di connessione configurata.</summary>
    public sealed class DbConnectionFactory : IDbConnectionFactory
    {
        public DbConnectionFactory(DatabaseProvider provider, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("La stringa di connessione è obbligatoria.", nameof(connectionString));

            Provider = provider;
            ConnectionString = connectionString;
        }

        public DatabaseProvider Provider { get; }

        public string ConnectionString { get; }

        public DbConnection CreateConnection()
        {
            switch (Provider)
            {
                case DatabaseProvider.Sqlite:
                    EnsureSqliteFolder();
                    return new SqliteConnection(ConnectionString);
                case DatabaseProvider.SqlServer:
                    return new SqlConnection(ConnectionString);
                default:
                    throw new NotSupportedException("Database non supportato: " + Provider);
            }
        }

        public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var connection = CreateConnection();
            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        /// <summary>SQLite non crea la cartella del file: lo si fa qui, così il primo avvio funziona senza preparativi.</summary>
        private void EnsureSqliteFolder()
        {
            var dataSource = new SqliteConnectionStringBuilder(ConnectionString).DataSource;
            if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:" || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                return;

            var folder = Path.GetDirectoryName(Path.GetFullPath(dataSource));
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);
        }
    }
}
