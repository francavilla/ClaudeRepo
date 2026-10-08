using System;
using System.Collections.Generic;
using System.Data;

namespace DesktopAppTemplate.Data.Dal
{
    /// <summary>
    /// *** FILE DA COMPLETARE *** Collega l'adattatore alla libreria DAL: ogni metodo deve richiamare il corrispondente
    /// wrapper di DAL. Finché non è completato, usare <c>--data-access dal</c> segnala l'errore qui sotto.
    ///
    /// Indicazioni (la connessione di DAL è un singleton con locking e le transazioni sono esplicite):
    ///  - ExecuteReader / ExecuteNonQuery: richiamare i wrapper di DAL passando SQL e parametri; i nomi dei parametri
    ///    arrivano già con il prefisso del dialetto (es. "@Id").
    ///  - BeginTransaction / Commit / Rollback: richiamare le chiamate esplicite di DAL.
    ///  - Non chiudere né eliminare la connessione di DAL: appartiene alla libreria.
    ///  - Non serve aggiungere altri lock: l'adattatore (<see cref="DalLock"/>) esegue una sola operazione per volta.
    ///    Se DAL espone il proprio oggetto di lock, usarlo in <see cref="DalLock"/> al posto del semaforo.
    /// </summary>
    public sealed class DalGateway : IDalGateway
    {
        public IDataReader ExecuteReader(string sql, IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            // TODO: return Dal.Instance.<wrapper di ExecuteReader>(sql, parameters...);
            throw NotLinked();
        }

        public int ExecuteNonQuery(string sql, IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            // TODO: return Dal.Instance.<wrapper di ExecuteNonQuery>(sql, parameters...);
            throw NotLinked();
        }

        public void BeginTransaction()
        {
            // TODO: Dal.Instance.<BeginTransaction>();
            throw NotLinked();
        }

        public void Commit()
        {
            // TODO: Dal.Instance.<Commit>();
            throw NotLinked();
        }

        public void Rollback()
        {
            // TODO: Dal.Instance.<Rollback>();
            throw NotLinked();
        }

        private static Exception NotLinked()
        {
            return new InvalidOperationException(
                "La libreria DAL non è ancora collegata: completa i metodi di DalGateway (src/DesktopAppTemplate.Data.Dal/DalGateway.cs).");
        }
    }
}
