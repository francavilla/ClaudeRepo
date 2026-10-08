using System.Data.Entity;
using Catalog.Application.Abstractions;
using Catalog.Domain.Products;
using Catalog.Infrastructure.Persistence.Configurations;

namespace Catalog.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext EF6. Implementa IUnitOfWork: una istanza per richiesta HTTP
    /// (InstancePerRequest in Autofac), condivisa da repository e query.
    /// </summary>
    public class CatalogDbContext : DbContext, IUnitOfWork
    {
        public const string ConnectionStringName = "CatalogDb";

        public CatalogDbContext()
            : this("name=" + ConnectionStringName)
        {
        }

        public CatalogDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            // Lazy loading e proxy disattivati: caricamenti espliciti e niente sorprese N+1.
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("catalog");
            modelBuilder.Configurations.Add(new ProductConfiguration());
        }
    }
}
