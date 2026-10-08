using System.Collections.Generic;

namespace Catalog.Application.Common
{
    public class PagedResult<T>
    {
        public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
        {
            Items = items;
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;
        }

        public IReadOnlyList<T> Items { get; private set; }

        public int Page { get; private set; }

        public int PageSize { get; private set; }

        public int TotalCount { get; private set; }
    }
}
