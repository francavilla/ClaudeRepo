using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using DesktopAppTemplate.Core.Data;
using Microsoft.Data.Sqlite;

namespace DesktopAppTemplate.Data.Dal
{
    /// <summary>
    /// Collega l'adattatore alla libreria DAL. Ogni metodo richiama il corrispondente wrapper di DAL, per ora rappresentato da
    /// <see cref="DalPlaceholder"/>: per collegare DAL basta sostituire <c>DalPlaceholder.</c> con la libreria vera (istruzioni e
    /// mappa dei segnaposto in <c>DalPlaceholder.cs</c>). Il resto dell'adattatore (serializzazione, transazioni, repository) non cambia.
    ///
    /// Punti di attenzione (la connessione di DAL è un singleton con locking e le transazioni sono esplicite):
    ///  - non chiudere né eliminare la connessione di DAL: appartiene alla libreria;
    ///  - non servono altri lock: <see cref="DalLock"/> esegue una sola operazione per volta
    ///    (se DAL espone un proprio oggetto di lock si può usare al suo posto del semaforo);
    ///  - i nomi dei parametri arrivano già con il prefisso del dialetto (es. "@Id").
    /// </summary>
    public sealed class DalGateway : IDalGateway
    {
        private readonly IDbConnectionFactory _connections;

        public DalGateway(IDbConnectionFactory connections)
        {
            _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        }

        public IDataReader ExecuteReader(string sql, IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            // SEGNAPOSTO: sostituire con il wrapper di ExecuteReader di DAL (es. Dal.Instance.ExecuteReader(sql, ...)).
            return DalPlaceholder.ExecuteReader(sql, CreateParameters(parameters));
        }

        public int ExecuteNonQuery(string sql, IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            // SEGNAPOSTO: sostituire con il wrapper di ExecuteNonQuery di DAL.
            return DalPlaceholder.ExecuteNonQuery(sql, CreateParameters(parameters));
        }

        public void BeginTransaction()
        {
            // SEGNAPOSTO: sostituire con l'apertura esplicita della transazione di DAL.
            DalPlaceholder.BeginTransaction();
        }

        public void Commit()
        {
            // SEGNAPOSTO: sostituire con la conferma di DAL.
            DalPlaceholder.Commit();
        }

        public void Rollback()
        {
            // SEGNAPOSTO: sostituire con l'annullamento di DAL.
            DalPlaceholder.Rollback();
        }

        /// <summary>
        /// SEGNAPOSTO: trasforma i parametri nel tipo che DAL si aspetta. Qui sono <see cref="DbParameter"/> del database in uso
        /// (come nei wrapper ADO.NET standard). Se DAL vuole altro (un dizionario, un suo tipo...) si cambia solo questo metodo.
        /// </summary>
        public DbParameter[] CreateParameters(IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            return (parameters ?? new List<KeyValuePair<string, object>>())
                .Select(p => CreateParameter(p.Key, p.Value ?? DBNull.Value))
                .ToArray();
        }

        private DbParameter CreateParameter(string name, object value)
        {
            switch (_connections.Provider)
            {
                case DatabaseProvider.SqlServer:
                    return new SqlParameter(name, value);
                default:
                    return new SqliteParameter(name, value);
            }
        }
    }
}
