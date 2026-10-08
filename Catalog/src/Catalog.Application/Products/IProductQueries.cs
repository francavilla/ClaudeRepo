using System;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Common;

namespace Catalog.Application.Products
{
    /// <summary>
    /// Lato lettura (CQRS "light"): proiezioni dirette su DTO, senza caricare l'aggregato.
    /// </summary>
    public interface IProductQueries
    {
        Task<ProductDto> FindByIdAsync(Guid id, CancellationToken cancellationToken);

        Task<PagedResult<ProductDto>> ListAsync(int page, int pageSize, bool includeInactive, CancellationToken cancellationToken);
    }
}
