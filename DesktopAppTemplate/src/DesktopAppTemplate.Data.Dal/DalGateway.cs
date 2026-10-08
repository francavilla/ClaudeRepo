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
    /// Collega l'adattatore alla libreria DAL. Ogni metodo richiama il corrispondente wrapper di DAL, per ora rappresentato da una DAL
    /// simulata con nomi fittizi (<see cref="DalPlaceholder"/>, già funzionante): per collegare la DAL vera basta sostituire le chiamate
    /// <c>_dal.</c> con quelle della libreria (istruzioni e mappa dei nomi in <c>DalPlaceholder.cs</c>). Il resto dell'adattatore
    /// (serializzazione, transazioni, repository) non cambia.
    ///
    /// Punti di attenzione (la connessione di DAL è un singleton con locking e le transazioni sono esplicite):
    ///  - non chiudere né eliminare la connessione di DAL: appartiene alla libreria;
    ///  - non servono altri lock: <see cref="DalLock"/> esegue una sola operazione per volta
    ///    (se DAL espone un proprio oggetto di lock si può usare al suo posto del semaforo);
    ///  - i nomi dei parametri arrivano già con il prefisso del dialetto (es. "@Id").
    /// </summary>
    public sealed class DalGateway : IDalGateway, IDisposable
    {
        private readonly IDbConnectionFactory _connections;

        // SEGNAPOSTO: la DAL simulata. Con la libreria vera questo campo sparisce (si usa il suo singleton, es. Dal.Instance).
        private readonly DalPlaceholder _dal;

        public DalGateway(IDbConnectionFactory connections)
        {
            _connections = connections ?? throw new ArgumentNullException(nameof(connections));
            _dal = new DalPlaceholder(connections);
        }

        /// <summary>Chiude la connessione della DAL simulata (con la DAL vera la connessione appartiene alla libreria e qui non si chiude nulla).</summary>
        public void Dispose() => _dal.Dispose();

        public IDataReader ExecuteReader(string sql, IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            // SEGNAPOSTO: sostituire `_dal.` con il wrapper di ExecuteReader di DAL (es. Dal.Instance.ExecuteReader(sql, ...)).
            return _dal.ExecuteReader(sql, CreateParameters(parameters));
        }

        public int ExecuteNonQuery(string sql, IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            // SEGNAPOSTO: sostituire `_dal.` con il wrapper di ExecuteNonQuery di DAL.
            return _dal.ExecuteNonQuery(sql, CreateParameters(parameters));
        }

        public void BeginTransaction()
        {
            // SEGNAPOSTO: sostituire `_dal.` con l'apertura esplicita della transazione di DAL.
            _dal.BeginTransaction();
        }

        public void Commit()
        {
            // SEGNAPOSTO: sostituire `_dal.` con la conferma di DAL.
            _dal.Commit();
        }

        public void Rollback()
        {
            // SEGNAPOSTO: sostituire `_dal.` con l'annullamento di DAL.
            _dal.Rollback();
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
