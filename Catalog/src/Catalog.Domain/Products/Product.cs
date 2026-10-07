using System;
using Catalog.Domain.Common;

namespace Catalog.Domain.Products
{
    /// <summary>
    /// Aggregate root del catalogo. Lo stato si modifica solo tramite metodi
    /// che garantiscono le invarianti (niente setter pubblici).
    /// </summary>
    public class Product : Entity
    {
        public const int NameMaxLength = 200;
        public const int SkuMaxLength = 32;

        // Richiesto da Entity Framework 6.
        protected Product()
        {
        }

        private Product(Guid id, string sku, string name, decimal price, DateTime createdOnUtc)
        {
            Id = id;
            Sku = sku;
            Name = name;
            Price = price;
            IsActive = true;
            CreatedOnUtc = createdOnUtc;
        }

        public string Sku { get; private set; }

        public string Name { get; private set; }

        public decimal Price { get; private set; }

        public bool IsActive { get; private set; }

        public DateTime CreatedOnUtc { get; private set; }

        public static Product Create(string sku, string name, decimal price, DateTime createdOnUtc)
        {
            sku = Guard.NotNullOrWhiteSpace(sku, "Lo SKU", SkuMaxLength).ToUpperInvariant();
            name = Guard.NotNullOrWhiteSpace(name, "Il nome", NameMaxLength);
            EnsureValidPrice(price);

            return new Product(Guid.NewGuid(), sku, name, price, createdOnUtc);
        }

        public void Rename(string name)
        {
            EnsureActive();
            Name = Guard.NotNullOrWhiteSpace(name, "Il nome", NameMaxLength);
        }

        public void ChangePrice(decimal newPrice)
        {
            EnsureActive();
            EnsureValidPrice(newPrice);
            Price = newPrice;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        private void EnsureActive()
        {
            if (!IsActive)
            {
                throw new DomainException("Il prodotto " + Sku + " è disattivato e non può essere modificato.");
            }
        }

        private static void EnsureValidPrice(decimal price)
        {
            if (price < 0m)
            {
                throw new DomainException("Il prezzo non può essere negativo.");
            }

            if (decimal.Round(price, 2) != price)
            {
                throw new DomainException("Il prezzo può avere al massimo due decimali.");
            }
        }
    }
}
