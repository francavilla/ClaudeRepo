using System;

namespace Catalog.Application.Products
{
    public class ProductDto
    {
        public Guid Id { get; set; }

        public string Sku { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedOnUtc { get; set; }
    }
}
