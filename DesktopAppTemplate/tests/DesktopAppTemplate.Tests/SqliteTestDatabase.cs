using System;
using System.IO;
using DesktopAppTemplate.Core.Data;
using DesktopAppTemplate.Data;
using Microsoft.Data.Sqlite;

namespace DesktopAppTemplate.Tests
{
    /// <summary>Database SQLite temporaneo (file in una cartella nuova) con lo schema già creato; si elimina da solo.</summary>
    internal sealed class SqliteTestDatabase : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "dat-db-" + Guid.NewGuid().ToString("N"));

        public SqliteTestDatabase(bool migrate = true)
        {
            FilePath = Path.Combine(_folder, "sub", "test.db");
            Connections = new DbConnectionFactory(DatabaseProvider.Sqlite, "Data Source=" + FilePath);

            if (migrate)
                new DatabaseMigrator(Connections, new SqlDialect()).MigrateAsync().GetAwaiter().GetResult();
        }

        public string FilePath { get; }

        public IDbConnectionFactory Connections { get; }

        public void Dispose()
        {
            // I pool di connessioni tengono il file aperto: vanno svuotati prima di eliminarlo.
            SqliteConnection.ClearAllPools();
            System.Data.SQLite.SQLiteConnection.ClearAllPools();

            try
            {
                if (Directory.Exists(_folder))
                    Directory.Delete(_folder, true);
            }
            catch (IOException)
            {
                // Cartella temporanea: se resta qualcosa, il sistema la ripulirà.
            }
        }
    }
}
