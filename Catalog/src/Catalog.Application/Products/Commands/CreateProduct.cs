using System;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Abstractions;
using Catalog.Application.Common;
using Catalog.Domain.Products;

namespace Catalog.Application.Products.Commands
{
    public class CreateProductCommand
    {
        public CreateProductCommand(string sku, string name, decimal price)
        {
            Sku = sku;
            Name = name;
            Price = price;
        }

        public string Sku { get; private set; }

        public string Name { get; private set; }

        public decimal Price { get; private set; }
    }

    public class CreateProductHandler : ICommandHandler<CreateProductCommand, Guid>
    {
        private readonly IProductRepository _products;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public CreateProductHandler(IProductRepository products, IUnitOfWork unitOfWork, IClock clock)
        {
            _products = products;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Guid> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            // Le invarianti del singolo prodotto sono nel Domain; l'unicità dello SKU
            // richiede di interrogare la persistenza, quindi è responsabilità del caso d'uso.
            var product = Product.Create(command.Sku, command.Name, command.Price, _clock.UtcNow);

            if (await _products.ExistsBySkuAsync(product.Sku, cancellationToken).ConfigureAwait(false))
            {
                throw new ValidationException("sku", "Esiste già un prodotto con SKU '" + product.Sku + "'.");
            }

            _products.Add(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return product.Id;
        }
    }
}
