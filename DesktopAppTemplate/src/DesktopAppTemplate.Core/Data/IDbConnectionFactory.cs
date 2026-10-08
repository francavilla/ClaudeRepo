using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopAppTemplate.Core.Data
{
    /// <summary>
    /// PUNTO DI AGGANCIO: come si ottiene una connessione. L'implementazione predefinita crea connessioni
    /// SQLite o SQL Server dalla stringa di connessione; una libreria esistente può fornire la propria.
    /// </summary>
    public interface IDbConnectionFactory
    {
        DatabaseProvider Provider { get; }

        string ConnectionString { get; }

        /// <summary>Crea una connessione chiusa (chi la riceve la apre e la elimina).</summary>
        DbConnection CreateConnection();

        /// <summary>Crea e apre una connessione (chi la riceve la elimina).</summary>
        Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
