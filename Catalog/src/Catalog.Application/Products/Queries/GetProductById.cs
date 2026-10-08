using System;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Abstractions;
using Catalog.Application.Common;

namespace Catalog.Application.Products.Queries
{
    public class GetProductByIdQuery
    {
        public GetProductByIdQuery(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
    }

    public class GetProductByIdHandler : IQueryHandler<GetProductByIdQuery, ProductDto>
    {
        private readonly IProductQueries _queries;

        public GetProductByIdHandler(IProductQueries queries)
        {
            _queries = queries;
        }

        public async Task<ProductDto> HandleAsync(GetProductByIdQuery query, CancellationToken cancellationToken)
        {
            var product = await _queries.FindByIdAsync(query.Id, cancellationToken).ConfigureAwait(false);
            if (product == null)
            {
                throw new NotFoundException("Prodotto", query.Id);
            }

            return product;
        }
    }
}
