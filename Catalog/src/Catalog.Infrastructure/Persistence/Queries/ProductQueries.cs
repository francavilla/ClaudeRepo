using System;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Common;
using Catalog.Application.Products;

namespace Catalog.Infrastructure.Persistence.Queries
{
    /// <summary>
    /// Lato lettura: AsNoTracking + proiezione in SQL direttamente sul DTO.
    /// </summary>
    public sealed class ProductQueries : IProductQueries
    {
        private readonly CatalogDbContext _context;

        public ProductQueries(CatalogDbContext context)
        {
            _context = context;
        }

        public Task<ProductDto> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Project(_context.Products.AsNoTracking().Where(p => p.Id == id))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<PagedResult<ProductDto>> ListAsync(int page, int pageSize, bool includeInactive, CancellationToken cancellationToken)
        {
            IQueryable<Domain.Products.Product> query = _context.Products.AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(p => p.IsActive);
            }

            var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var items = await Project(query
                    .OrderBy(p => p.Name)
                    .ThenBy(p => p.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new PagedResult<ProductDto>(items, page, pageSize, total);
        }

        private static IQueryable<ProductDto> Project(IQueryable<Domain.Products.Product> source)
        {
            return source.Select(p => new ProductDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Price = p.Price,
                IsActive = p.IsActive,
                CreatedOnUtc = p.CreatedOnUtc
            });
        }
    }
}
