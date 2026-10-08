using System.Collections.Generic;
using System.Data;

namespace DesktopAppTemplate.Data.Dal
{
    /// <summary>
    /// Le sole operazioni che servono alla libreria DAL, nella forma più semplice possibile. È l'unico punto che deve
    /// conoscere DAL: <see cref="DalGateway"/> traduce queste chiamate nei metodi reali della libreria.
    /// Le chiamate sono sincrone (come i wrapper ADO.NET di DAL); asincronia e serializzazione le gestisce l'adattatore.
    /// </summary>
    public interface IDalGateway
    {
        /// <summary>
        /// Esegue una SELECT sulla connessione singleton e restituisce il reader (l'adattatore lo legge tutto e lo chiude).
        /// </summary>
        /// <param name="sql">SQL già adattato al dialetto, con parametri nominati.</param>
        /// <param name="parameters">Nome (con il prefisso del dialetto, es. "@Id") e valore.</param>
        IDataReader ExecuteReader(string sql, IReadOnlyList<KeyValuePair<string, object>> parameters);

        /// <summary>Esegue INSERT, UPDATE, DELETE... e restituisce le righe interessate.</summary>
        int ExecuteNonQuery(string sql, IReadOnlyList<KeyValuePair<string, object>> parameters);

        /// <summary>Apre una transazione esplicita sulla connessione singleton.</summary>
        void BeginTransaction();

        void Commit();

        void Rollback();
    }
}
