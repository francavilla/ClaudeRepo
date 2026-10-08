using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Data;
using DesktopAppTemplate.Data;
using DesktopAppTemplate.Data.Tasks;
using DesktopAppTemplate.Features.Tasks;
using Xunit;

namespace DesktopAppTemplate.Tests
{
    /// <summary>Esegue i test solo se è indicata una connessione SQL Server di prova (altrimenti risultano saltati).</summary>
    public sealed class SqlServerFactAttribute : FactAttribute
    {
        public const string Variable = "DESKTOPAPPTEMPLATE_SQLSERVER";

        public SqlServerFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable)))
                Skip = "Per eseguire questo test imposta la variabile d'ambiente " + Variable +
                       " con la stringa di connessione di un database SQL Server di prova (le tabelle Tasks e SchemaVersion vengono create se mancano).";
        }
    }

    public class SqlServerTests
    {
        private static readonly CancellationToken None = default(CancellationToken);

        private static IDbConnectionFactory Connections()
        {
            return new DbConnectionFactory(DatabaseProvider.SqlServer, Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable));
        }

        private static async Task ExerciseAsync(ITaskRepository repository)
        {
            var task = new TaskItem(Guid.NewGuid(), "Prova SQL Server", new DateTime(2026, 10, 8, 9, 30, 15, DateTimeKind.Local));

            await repository.AddAsync(task, None);
            try
            {
                var found = await repository.GetAsync(task.Id, None);
                Assert.Equal("Prova SQL Server", found.Title);
                Assert.Equal(task.CreatedAt, found.CreatedAt);

                await repository.UpdateAsync(task.WithCompleted(true), None);
                Assert.True((await repository.GetAsync(task.Id, None)).IsCompleted);
            }
            finally
            {
                await repository.RemoveAsync(task.Id, None);
            }

            Assert.Null(await repository.GetAsync(task.Id, None));
        }

        [SqlServerFact]
        public async Task Migrazione_e_repository_ADO_su_SQL_Server()
        {
            var connections = Connections();
            await new DatabaseMigrator(connections, new SqlDialect()).MigrateAsync();

            await ExerciseAsync(new AdoNetTaskRepository(new AdoNetExecutor(connections, new SqlDialect())));
        }

        [SqlServerFact]
        public async Task Il_database_mancante_viene_creato_e_poi_lo_schema_si_applica()
        {
            var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable));
            var name = "ClaudeDB_test_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            builder.InitialCatalog = name;
            var connections = new DbConnectionFactory(DatabaseProvider.SqlServer, builder.ConnectionString);

            try
            {
                var result = await new DatabaseMigrator(connections, new SqlDialect()).MigrateAsync();
                Assert.True(result.IsNewDatabase);

                await DatabaseCreator.EnsureExistsAsync(connections);   // già esistente: non deve fallire né ricreare nulla
                await ExerciseAsync(new AdoNetTaskRepository(new AdoNetExecutor(connections, new SqlDialect())));
            }
            finally
            {
                SqlConnection.ClearAllPools();
                builder.InitialCatalog = "master";
                using (var master = new SqlConnection(builder.ConnectionString))
                {
                    master.Open();
                    using (var command = master.CreateCommand())
                    {
                        command.CommandText = "IF DB_ID(@name) IS NOT NULL BEGIN DECLARE @sql NVARCHAR(MAX) = N'ALTER DATABASE ' + QUOTENAME(@name) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ' + QUOTENAME(@name); EXEC (@sql); END";
                        command.Parameters.AddWithValue("@name", name);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
