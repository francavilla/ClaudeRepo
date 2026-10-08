using System;
using System.Data;
using System.Data.Common;
using DesktopAppTemplate.Core.Data;

namespace DesktopAppTemplate.Data.Dal
{
    /// <summary>
    /// *** DAL SIMULATA (nomi fittizi) DA SOSTITUIRE *** Si comporta come la libreria DAL descritta: una sola connessione per tutta
    /// l'applicazione (singleton) protetta da blocco, metodi che incapsulano quelli standard di ADO.NET, transazioni esplicite
    /// (senza <see cref="BeginTransaction"/> ogni comando è confermato subito). Così <c>--data-access dal</c> funziona già, senza la libreria vera.
    ///
    /// COME COLLEGARE LA DAL VERA (4 passi):
    ///  1. aggiungere in <c>DesktopAppTemplate.Data.Dal.csproj</c> il riferimento all'assembly di DAL;
    ///  2. in <see cref="DalGateway"/> sostituire le chiamate <c>_dal.</c> con quelle reali (es. <c>Dal.Instance.</c>);
    ///  3. se i nomi o i parametri di DAL sono diversi, adattare solo le righe di <see cref="DalGateway"/> che li usano
    ///     (e, se DAL non usa <see cref="DbParameter"/>, il metodo <see cref="DalGateway.CreateParameters"/>);
    ///  4. eliminare questo file (e il campo <c>_dal</c> in <see cref="DalGateway"/>).
    ///
    /// MAPPA DEI NOMI FITTIZI (cosa cercare in DAL):
    ///  - <see cref="ExecuteReader"/>    ->  il wrapper di ExecuteReader (SELECT che restituisce un reader)
    ///  - <see cref="ExecuteNonQuery"/>  ->  il wrapper di ExecuteNonQuery (INSERT, UPDATE, DELETE: restituisce le righe interessate)
    ///  - <see cref="BeginTransaction"/> ->  l'apertura esplicita della transazione
    ///  - <see cref="Commit"/>           ->  la conferma
    ///  - <see cref="Rollback"/>         ->  l'annullamento
    /// </summary>
    internal sealed class DalPlaceholder : IDisposable
    {
        private readonly IDbConnectionFactory _connections;
        private readonly object _sync = new object();
        private DbConnection _connection;
        private DbTransaction _transaction;
        private bool _disposed;

        public DalPlaceholder(IDbConnectionFactory connections)
        {
            _connections = connections;
        }

        /// <summary>Esegue una SELECT. Le righe vengono copiate subito in un reader autonomo: la connessione condivisa si libera senza attendere chi legge.</summary>
        public IDataReader ExecuteReader(string sql, params DbParameter[] parameters)
        {
            lock (_sync)
            {
                using (var command = CreateCommand(sql, parameters))
                using (var reader = command.ExecuteReader())
                {
                    var table = new DataTable();
                    for (var i = 0; i < reader.FieldCount; i++)
                        table.Columns.Add(reader.GetName(i), typeof(object));

                    while (reader.Read())
                    {
                        var values = new object[reader.FieldCount];
                        reader.GetValues(values);
                        table.Rows.Add(values);
                    }

                    return table.CreateDataReader();
                }
            }
        }

        public int ExecuteNonQuery(string sql, params DbParameter[] parameters)
        {
            lock (_sync)
            {
                using (var command = CreateCommand(sql, parameters))
                    return command.ExecuteNonQuery();
            }
        }

        public void BeginTransaction()
        {
            lock (_sync)
            {
                if (_transaction != null)
                    throw new InvalidOperationException("Una transazione è già aperta sulla connessione condivisa.");

                _transaction = Connection().BeginTransaction();
            }
        }

        public void Commit()
        {
            lock (_sync)
            {
                var transaction = TakeTransaction();
                using (transaction)
                    transaction.Commit();
            }
        }

        public void Rollback()
        {
            lock (_sync)
            {
                var transaction = TakeTransaction();
                using (transaction)
                    transaction.Rollback();
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _disposed = true;
                _transaction?.Dispose();
                _transaction = null;
                _connection?.Dispose();
                _connection = null;
            }
        }

        private DbTransaction TakeTransaction()
        {
            var transaction = _transaction;
            if (transaction == null)
                throw new InvalidOperationException("Nessuna transazione aperta: chiama BeginTransaction prima di Commit o Rollback.");

            _transaction = null;
            return transaction;
        }

        private DbCommand CreateCommand(string sql, DbParameter[] parameters)
        {
            var command = Connection().CreateCommand();
            command.CommandText = sql;
            command.Transaction = _transaction;
            foreach (var parameter in parameters)
                command.Parameters.Add(parameter);
            return command;
        }

        /// <summary>La connessione è una sola, aperta alla prima richiesta e tenuta per tutta la vita dell'applicazione.</summary>
        private DbConnection Connection()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DalPlaceholder));

            if (_connection == null)
            {
                _connection = _connections.CreateConnection();
                _connection.Open();
            }

            return _connection;
        }
    }
}
