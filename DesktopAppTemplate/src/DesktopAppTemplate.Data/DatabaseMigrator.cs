using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Data;

namespace DesktopAppTemplate.Data
{
    /// <summary>
    /// Applica gli script <c>Scripts\&lt;Database&gt;\Vnnn_Nome.sql</c> (incorporati nell'assembly) non ancora eseguiti,
    /// in ordine di versione e ciascuno in una transazione. La versione corrente è nella tabella SchemaVersion.
    /// Per modificare lo schema si aggiunge un nuovo script (V002_...), non si cambia mai uno già applicato.
    /// </summary>
    public sealed class DatabaseMigrator : IDatabaseMigrator
    {
        private static readonly Regex ScriptName = new Regex(@"^V(?<version>\d+)_(?<name>.+)\.sql$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IDbConnectionFactory _connections;
        private readonly ISqlDialect _dialect;
        private readonly Assembly _scriptsAssembly;

        public DatabaseMigrator(IDbConnectionFactory connections, ISqlDialect dialect)
            : this(connections, dialect, typeof(DatabaseMigrator).Assembly)
        {
        }

        public DatabaseMigrator(IDbConnectionFactory connections, ISqlDialect dialect, Assembly scriptsAssembly)
        {
            _connections = connections;
            _dialect = dialect;
            _scriptsAssembly = scriptsAssembly;
        }

        public async Task<MigrationResult> MigrateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var scripts = LoadScripts();

            using (var connection = await _connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                await ExecuteAsync(connection, null, VersionTableSql(), null, cancellationToken).ConfigureAwait(false);

                var current = Convert.ToInt32(await ScalarAsync(connection, "SELECT COALESCE(MAX(Version), 0) FROM SchemaVersion", cancellationToken).ConfigureAwait(false));
                var applied = 0;

                foreach (var script in scripts.Where(s => s.Version > current))
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        await ExecuteAsync(connection, transaction, script.Sql, null, cancellationToken).ConfigureAwait(false);
                        await ExecuteAsync(connection, transaction,
                            _dialect.Adapt("INSERT INTO SchemaVersion (Version, Script, AppliedAt) VALUES (@Version, @Script, @AppliedAt)"),
                            new Dictionary<string, object>
                            {
                                { "Version", (long)script.Version },
                                { "Script", script.Name },
                                { "AppliedAt", DateTime.Now.ToString("o") }
                            }, cancellationToken).ConfigureAwait(false);
                        transaction.Commit();
                    }

                    applied++;
                }

                return new MigrationResult(current, applied);
            }
        }

        private string VersionTableSql()
        {
            switch (_connections.Provider)
            {
                case DatabaseProvider.SqlServer:
                    return "IF OBJECT_ID(N'dbo.SchemaVersion', N'U') IS NULL CREATE TABLE dbo.SchemaVersion (" +
                           "Version INT NOT NULL CONSTRAINT PK_SchemaVersion PRIMARY KEY, Script NVARCHAR(200) NOT NULL, AppliedAt NVARCHAR(40) NOT NULL)";
                default:
                    return "CREATE TABLE IF NOT EXISTS SchemaVersion (Version INTEGER NOT NULL PRIMARY KEY, Script TEXT NOT NULL, AppliedAt TEXT NOT NULL)";
            }
        }

        private async Task<object> ScalarAsync(DbConnection connection, string sql, CancellationToken cancellationToken)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, string sql,
            IDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = transaction;
                if (parameters != null)
                {
                    foreach (var pair in parameters)
                        command.AddParameter(_dialect.ParameterPrefix + pair.Key, pair.Value);
                }

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private List<MigrationScript> LoadScripts()
        {
            var folder = _connections.Provider == DatabaseProvider.SqlServer ? "SqlServer" : "Sqlite";
            var marker = ".Scripts." + folder + ".";

            var scripts = new List<MigrationScript>();
            foreach (var resource in _scriptsAssembly.GetManifestResourceNames())
            {
                var index = resource.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index < 0 || !resource.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileName = resource.Substring(index + marker.Length);
                var match = ScriptName.Match(fileName);
                if (!match.Success)
                    throw new InvalidOperationException($"Nome dello script non valido: {fileName} (atteso Vnnn_Nome.sql).");

                using (var stream = _scriptsAssembly.GetManifestResourceStream(resource))
                using (var reader = new StreamReader(stream))
                    scripts.Add(new MigrationScript(int.Parse(match.Groups["version"].Value), fileName, reader.ReadToEnd()));
            }

            var duplicate = scripts.GroupBy(s => s.Version).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
                throw new InvalidOperationException($"Più script con la stessa versione {duplicate.Key}.");

            return scripts.OrderBy(s => s.Version).ToList();
        }

        private sealed class MigrationScript
        {
            public MigrationScript(int version, string name, string sql)
            {
                Version = version;
                Name = name;
                Sql = sql;
            }

            public int Version { get; }
            public string Name { get; }
            public string Sql { get; }
        }
    }
}
