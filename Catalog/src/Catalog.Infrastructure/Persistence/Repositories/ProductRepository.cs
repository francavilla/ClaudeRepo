using System;
using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Domain.Products;

namespace Catalog.Infrastructure.Persistence.Repositories
{
    public sealed class ProductRepository : IProductRepository
    {
        private readonly CatalogDbContext _context;

        public ProductRepository(CatalogDbContext context)
        {
            _context = context;
        }

        public Task<Product> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken)
        {
            return _context.Products.AnyAsync(p => p.Sku == sku, cancellationToken);
        }

        public void Add(Product product)
        {
            _context.Products.Add(product);
        }
    }
}
