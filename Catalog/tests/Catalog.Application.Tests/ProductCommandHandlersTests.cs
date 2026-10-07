using System;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Abstractions;
using Catalog.Application.Common;
using Catalog.Application.Products.Commands;
using Catalog.Application.Products.Queries;
using Catalog.Application.Products;
using Catalog.Domain.Products;
using Moq;
using Xunit;

namespace Catalog.Application.Tests
{
    public class ProductCommandHandlersTests
    {
        private readonly Mock<IProductRepository> _products = new Mock<IProductRepository>();
        private readonly Mock<IUnitOfWork> _unitOfWork = new Mock<IUnitOfWork>();

        [Fact]
        public async Task ChangePrice_su_prodotto_inesistente_lancia_NotFoundException()
        {
            var handler = new ChangeProductPriceHandler(_products.Object, _unitOfWork.Object);

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.HandleAsync(new ChangeProductPriceCommand(Guid.NewGuid(), 10m), CancellationToken.None));

            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ChangePrice_aggiorna_e_salva()
        {
            var product = Product.Create("SKU", "Nome", 10m, DateTime.UtcNow);
            _products.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
            var handler = new ChangeProductPriceHandler(_products.Object, _unitOfWork.Object);

            await handler.HandleAsync(new ChangeProductPriceCommand(product.Id, 15m), CancellationToken.None);

            Assert.Equal(15m, product.Price);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Deactivate_disattiva_e_salva()
        {
            var product = Product.Create("SKU", "Nome", 10m, DateTime.UtcNow);
            _products.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
            var handler = new DeactivateProductHandler(_products.Object, _unitOfWork.Object);

            await handler.HandleAsync(new DeactivateProductCommand(product.Id), CancellationToken.None);

            Assert.False(product.IsActive);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(0, 20)]
        [InlineData(1, 0)]
        [InlineData(1, ListProductsQuery.MaxPageSize + 1)]
        public async Task ListProducts_con_paginazione_non_valida_lancia_ValidationException(int page, int pageSize)
        {
            var queries = new Mock<IProductQueries>();
            var handler = new ListProductsHandler(queries.Object);

            await Assert.ThrowsAsync<ValidationException>(
                () => handler.HandleAsync(new ListProductsQuery(page, pageSize, false), CancellationToken.None));
        }
    }
}
