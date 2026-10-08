using System;
using Catalog.Domain.Common;
using Catalog.Domain.Products;
using Xunit;

namespace Catalog.Domain.Tests
{
    public class ProductTests
    {
        private static readonly DateTime Now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Create_normalizza_sku_e_nome_e_il_prodotto_e_attivo()
        {
            var product = Product.Create("  abc-001 ", "  Tastiera  ", 49.90m, Now);

            Assert.NotEqual(Guid.Empty, product.Id);
            Assert.Equal("ABC-001", product.Sku);
            Assert.Equal("Tastiera", product.Name);
            Assert.Equal(49.90m, product.Price);
            Assert.True(product.IsActive);
            Assert.Equal(Now, product.CreatedOnUtc);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_senza_sku_lancia_DomainException(string sku)
        {
            Assert.Throws<DomainException>(() => Product.Create(sku, "Nome", 1m, Now));
        }

        [Fact]
        public void Create_con_nome_troppo_lungo_lancia_DomainException()
        {
            var name = new string('x', Product.NameMaxLength + 1);

            Assert.Throws<DomainException>(() => Product.Create("SKU", name, 1m, Now));
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(10.123)]
        public void Create_con_prezzo_non_valido_lancia_DomainException(double price)
        {
            Assert.Throws<DomainException>(() => Product.Create("SKU", "Nome", (decimal)price, Now));
        }

        [Fact]
        public void ChangePrice_aggiorna_il_prezzo()
        {
            var product = Product.Create("SKU", "Nome", 10m, Now);

            product.ChangePrice(12.50m);

            Assert.Equal(12.50m, product.Price);
        }

        [Fact]
        public void Un_prodotto_disattivato_non_puo_essere_modificato()
        {
            var product = Product.Create("SKU", "Nome", 10m, Now);
            product.Deactivate();

            Assert.False(product.IsActive);
            Assert.Throws<DomainException>(() => product.ChangePrice(20m));
            Assert.Throws<DomainException>(() => product.Rename("Altro"));
        }

        [Fact]
        public void Due_entita_con_lo_stesso_id_sono_uguali()
        {
            var product = Product.Create("SKU", "Nome", 10m, Now);

            Assert.Equal(product, product);
            Assert.NotEqual(product, Product.Create("SKU", "Nome", 10m, Now));
        }
    }
}
