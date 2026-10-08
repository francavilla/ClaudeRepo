using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopAppTemplate.Core.Data
{
    /// <summary>
    /// PUNTO DI AGGANCIO PRINCIPALE per ADO.NET: esegue SQL con parametri nominati. I repository ADO.NET del modello
    /// usano solo questa interfaccia: per usare una libreria esistente basta scrivere un adattatore che la implementa
    /// (e registrarlo al posto di quello predefinito), senza toccare i repository.
    /// </summary>
    public interface IDbExecutor
    {
        /// <summary>Esegue una query e trasforma ogni riga con <paramref name="map"/>.</summary>
        /// <param name="sql">Testo SQL con parametri nella forma <c>@Nome</c>.</param>
        /// <param name="parameters">Valori per nome, senza prefisso (es. "Id"); può essere null.</param>
        Task<IReadOnlyList<T>> QueryAsync<T>(string sql, IDictionary<string, object> parameters, Func<IDataRecord, T> map,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>Esegue un comando (INSERT, UPDATE, DELETE...) e restituisce le righe interessate.</summary>
        Task<int> ExecuteAsync(string sql, IDictionary<string, object> parameters = null,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
