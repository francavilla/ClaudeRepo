using System;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Abstractions;
using Catalog.Application.Common;
using Catalog.Domain.Products;

namespace Catalog.Application.Products.Commands
{
    public class ChangeProductPriceCommand
    {
        public ChangeProductPriceCommand(Guid productId, decimal newPrice)
        {
            ProductId = productId;
            NewPrice = newPrice;
        }

        public Guid ProductId { get; private set; }

        public decimal NewPrice { get; private set; }
    }

    public class ChangeProductPriceHandler : ICommandHandler<ChangeProductPriceCommand>
    {
        private readonly IProductRepository _products;
        private readonly IUnitOfWork _unitOfWork;

        public ChangeProductPriceHandler(IProductRepository products, IUnitOfWork unitOfWork)
        {
            _products = products;
            _unitOfWork = unitOfWork;
        }

        public async Task HandleAsync(ChangeProductPriceCommand command, CancellationToken cancellationToken)
        {
            var product = await _products.GetByIdAsync(command.ProductId, cancellationToken).ConfigureAwait(false);
            if (product == null)
            {
                throw new NotFoundException("Prodotto", command.ProductId);
            }

            product.ChangePrice(command.NewPrice);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
