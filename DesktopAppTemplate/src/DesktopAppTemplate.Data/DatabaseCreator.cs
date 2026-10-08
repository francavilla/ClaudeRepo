using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Data;

namespace DesktopAppTemplate.Data
{
    /// <summary>
    /// Crea il database se non esiste. Serve solo a SQL Server (SQLite crea il file da solo): ci si collega al database di sistema
    /// <c>master</c> e si esegue <c>CREATE DATABASE</c> solo se il nome indicato nella stringa di connessione non esiste ancora.
    /// Richiede il permesso di creare database; se manca, l'errore dice come procedere.
    /// </summary>
    public static class DatabaseCreator
    {
        public static async Task EnsureExistsAsync(IDbConnectionFactory connections, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (connections.Provider != DatabaseProvider.SqlServer)
                return;

            string database = null;
            string server = null;
            try
            {
                var builder = new SqlConnectionStringBuilder(connections.ConnectionString);
                database = builder.InitialCatalog;
                server = builder.DataSource;
                if (string.IsNullOrWhiteSpace(database))
                    return;   // nessun database indicato: ci si collega a quello predefinito dell'utente

                builder.InitialCatalog = "master";
                using (var connection = new SqlConnection(builder.ConnectionString))
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    using (var command = connection.CreateCommand())
                    {
                        // Il nome si passa come parametro e si quota con QUOTENAME: nessuna concatenazione di testo non controllato.
                        command.CommandText = "IF DB_ID(@name) IS NULL BEGIN DECLARE @sql NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(@name); EXEC (@sql); END";
                        command.AddParameter("@name", database);
                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw new InvalidOperationException(
                    "Impossibile verificare o creare il database \"" + database + "\" sul server \"" + server + "\": " + ex.Message +
                    " Serve il permesso di creare database (CREATE DATABASE): in alternativa fai creare il database a chi amministra il server " +
                    "e avvia l'app con --no-migrate, oppure usa --storage sqlite.", ex);
            }
        }
    }
}
