using System;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Abstractions;
using Catalog.Application.Common;
using Catalog.Application.Products.Commands;
using Catalog.Domain.Common;
using Catalog.Domain.Products;
using Moq;
using Xunit;

namespace Catalog.Application.Tests
{
    public class CreateProductHandlerTests
    {
        private static readonly DateTime Now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly Mock<IProductRepository> _products = new Mock<IProductRepository>();
        private readonly Mock<IUnitOfWork> _unitOfWork = new Mock<IUnitOfWork>();
        private readonly Mock<IClock> _clock = new Mock<IClock>();
        private readonly CreateProductHandler _handler;

        public CreateProductHandlerTests()
        {
            _clock.SetupGet(c => c.UtcNow).Returns(Now);
            _handler = new CreateProductHandler(_products.Object, _unitOfWork.Object, _clock.Object);
        }

        [Fact]
        public async Task Crea_il_prodotto_e_salva_una_sola_volta()
        {
            Product added = null;
            _products.Setup(r => r.Add(It.IsAny<Product>())).Callback<Product>(p => added = p);

            var id = await _handler.HandleAsync(new CreateProductCommand("sku-1", "Mouse", 19.99m), CancellationToken.None);

            Assert.NotNull(added);
            Assert.Equal(id, added.Id);
            Assert.Equal("SKU-1", added.Sku);
            Assert.Equal(Now, added.CreatedOnUtc);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Sku_duplicato_lancia_ValidationException_e_non_salva()
        {
            _products.Setup(r => r.ExistsBySkuAsync("SKU-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _handler.HandleAsync(new CreateProductCommand("sku-1", "Mouse", 19.99m), CancellationToken.None));

            Assert.True(ex.Errors.ContainsKey("sku"));
            _products.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Input_che_viola_il_dominio_non_interroga_la_persistenza()
        {
            await Assert.ThrowsAsync<DomainException>(
                () => _handler.HandleAsync(new CreateProductCommand("sku-1", "Mouse", -1m), CancellationToken.None));

            _products.Verify(r => r.ExistsBySkuAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
