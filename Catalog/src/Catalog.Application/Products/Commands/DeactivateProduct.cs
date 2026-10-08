using System;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Abstractions;
using Catalog.Application.Common;
using Catalog.Domain.Products;

namespace Catalog.Application.Products.Commands
{
    public class DeactivateProductCommand
    {
        public DeactivateProductCommand(Guid productId)
        {
            ProductId = productId;
        }

        public Guid ProductId { get; private set; }
    }

    public class DeactivateProductHandler : ICommandHandler<DeactivateProductCommand>
    {
        private readonly IProductRepository _products;
        private readonly IUnitOfWork _unitOfWork;

        public DeactivateProductHandler(IProductRepository products, IUnitOfWork unitOfWork)
        {
            _products = products;
            _unitOfWork = unitOfWork;
        }

        public async Task HandleAsync(DeactivateProductCommand command, CancellationToken cancellationToken)
        {
            var product = await _products.GetByIdAsync(command.ProductId, cancellationToken).ConfigureAwait(false);
            if (product == null)
            {
                throw new NotFoundException("Prodotto", command.ProductId);
            }

            product.Deactivate();
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
