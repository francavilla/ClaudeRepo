using System;
using System.Threading;
using System.Threading.Tasks;

namespace Catalog.Domain.Products
{
    /// <summary>
    /// Repository dell'aggregato (lato scrittura). L'implementazione vive in Infrastructure.
    /// </summary>
    public interface IProductRepository
    {
        Task<Product> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken);

        void Add(Product product);
    }
}
