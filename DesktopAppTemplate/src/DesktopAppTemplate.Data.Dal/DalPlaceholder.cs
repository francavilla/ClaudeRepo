using System;
using System.Data;
using System.Data.Common;

namespace DesktopAppTemplate.Data.Dal
{
    /// <summary>
    /// *** SEGNAPOSTO DA SOSTITUIRE *** Rappresenta la libreria DAL con la forma più probabile dei suoi wrapper ADO.NET:
    /// connessione singleton (già gestita da DAL) e transazioni esplicite. Finché DAL non è collegata ogni metodo segnala l'errore.
    ///
    /// COME COLLEGARE DAL (4 passi):
    ///  1. aggiungere in <c>DesktopAppTemplate.Data.Dal.csproj</c> il riferimento all'assembly di DAL;
    ///  2. in <see cref="DalGateway"/> sostituire <c>DalPlaceholder.</c> con la chiamata reale (es. <c>Dal.Instance.</c>);
    ///  3. se i nomi o i parametri di DAL sono diversi, adattare solo le righe di <see cref="DalGateway"/> che li usano
    ///     (e, se DAL non usa <see cref="DbParameter"/>, il metodo <see cref="DalGateway.CreateParameters"/>);
    ///  4. eliminare questo file.
    ///
    /// MAPPA DEI SEGNAPOSTO (cosa cercare in DAL):
    ///  - <see cref="ExecuteReader"/>    ->  il wrapper di ExecuteReader (SELECT che restituisce un reader)
    ///  - <see cref="ExecuteNonQuery"/>  ->  il wrapper di ExecuteNonQuery (INSERT, UPDATE, DELETE: restituisce le righe interessate)
    ///  - <see cref="BeginTransaction"/> ->  l'apertura esplicita della transazione
    ///  - <see cref="Commit"/>           ->  la conferma
    ///  - <see cref="Rollback"/>         ->  l'annullamento
    /// </summary>
    internal static class DalPlaceholder
    {
        public static IDataReader ExecuteReader(string sql, params DbParameter[] parameters) => throw NotLinked();

        public static int ExecuteNonQuery(string sql, params DbParameter[] parameters) => throw NotLinked();

        public static void BeginTransaction() => throw NotLinked();

        public static void Commit() => throw NotLinked();

        public static void Rollback() => throw NotLinked();

        private static Exception NotLinked()
        {
            return new InvalidOperationException(
                "La libreria DAL non è ancora collegata: sostituisci DalPlaceholder in DalGateway (src/DesktopAppTemplate.Data.Dal/DalGateway.cs) " +
                "con le chiamate reali della libreria.");
        }
    }
}
