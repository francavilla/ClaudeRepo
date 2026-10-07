using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Abstractions;
using Catalog.Application.Common;

namespace Catalog.Application.Products.Queries
{
    public class ListProductsQuery
    {
        public const int MaxPageSize = 100;

        public ListProductsQuery(int page, int pageSize, bool includeInactive)
        {
            Page = page;
            PageSize = pageSize;
            IncludeInactive = includeInactive;
        }

        public int Page { get; private set; }

        public int PageSize { get; private set; }

        public bool IncludeInactive { get; private set; }
    }

    public class ListProductsHandler : IQueryHandler<ListProductsQuery, PagedResult<ProductDto>>
    {
        private readonly IProductQueries _queries;

        public ListProductsHandler(IProductQueries queries)
        {
            _queries = queries;
        }

        public Task<PagedResult<ProductDto>> HandleAsync(ListProductsQuery query, CancellationToken cancellationToken)
        {
            var errors = new Dictionary<string, string>();
            if (query.Page < 1)
            {
                errors.Add("page", "Deve essere maggiore o uguale a 1.");
            }

            if (query.PageSize < 1 || query.PageSize > ListProductsQuery.MaxPageSize)
            {
                errors.Add("pageSize", "Deve essere compreso tra 1 e " + ListProductsQuery.MaxPageSize + ".");
            }

            if (errors.Count > 0)
            {
                throw new ValidationException(errors);
            }

            return _queries.ListAsync(query.Page, query.PageSize, query.IncludeInactive, cancellationToken);
        }
    }
}
