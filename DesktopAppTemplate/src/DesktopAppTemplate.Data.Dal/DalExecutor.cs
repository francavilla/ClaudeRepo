using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Data;

namespace DesktopAppTemplate.Data.Dal
{
    /// <summary>
    /// <see cref="IDbExecutor"/> che passa da DAL: adatta SQL e parametri al dialetto, serializza l'accesso alla connessione
    /// singleton (<see cref="DalLock"/>) e delega l'esecuzione a <see cref="IDalGateway"/>. Con questo registrato al posto
    /// di quello predefinito, i repository ADO.NET usano DAL senza altre modifiche.
    /// </summary>
    public sealed class DalExecutor : IDbExecutor
    {
        private readonly IDalGateway _gateway;
        private readonly ISqlDialect _dialect;
        private readonly DalLock _lock;

        public DalExecutor(IDalGateway gateway, ISqlDialect dialect, DalLock dalLock)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
            _lock = dalLock ?? throw new ArgumentNullException(nameof(dalLock));
        }

        public Task<IReadOnlyList<T>> QueryAsync<T>(string sql, IDictionary<string, object> parameters, Func<IDataRecord, T> map,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var text = _dialect.Adapt(sql);
            var list = Prepare(parameters);

            return _lock.RunAsync<IReadOnlyList<T>>(() =>
            {
                using (var reader = _gateway.ExecuteReader(text, list))
                {
                    var rows = new List<T>();
                    while (reader.Read())
                        rows.Add(map(reader));
                    return rows;
                }
            }, cancellationToken);
        }

        public Task<int> ExecuteAsync(string sql, IDictionary<string, object> parameters = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var text = _dialect.Adapt(sql);
            var list = Prepare(parameters);

            return _lock.RunAsync(() => _gateway.ExecuteNonQuery(text, list), cancellationToken);
        }

        private IReadOnlyList<KeyValuePair<string, object>> Prepare(IDictionary<string, object> parameters)
        {
            return (parameters ?? new Dictionary<string, object>())
                .Select(p => new KeyValuePair<string, object>(_dialect.ParameterPrefix + p.Key, p.Value))
                .ToList();
        }
    }
}
