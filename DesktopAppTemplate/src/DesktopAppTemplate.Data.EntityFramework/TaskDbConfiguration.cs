using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.SqlServer;
using System.Data.SQLite;
using System.Data.SQLite.EF6;

namespace DesktopAppTemplate.Data.EntityFramework
{
    /// <summary>
    /// Registra in codice i provider di EF6 (SQL Server e SQLite), così non serve nulla in App.config.
    /// </summary>
    internal sealed class TaskDbConfiguration : DbConfiguration
    {
        public TaskDbConfiguration()
        {
            SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);

            SetProviderFactory("System.Data.SQLite", SQLiteFactory.Instance);
            SetProviderFactory("System.Data.SQLite.EF6", SQLiteProviderFactory.Instance);
            SetProviderServices("System.Data.SQLite", (DbProviderServices)SQLiteProviderFactory.Instance.GetService(typeof(DbProviderServices)));
        }
    }
}
