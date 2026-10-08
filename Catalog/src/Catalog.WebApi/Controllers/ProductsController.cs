using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Catalog.Application.Abstractions;
using Catalog.Application.Common;
using Catalog.Application.Products;
using Catalog.Application.Products.Commands;
using Catalog.Application.Products.Queries;
using Catalog.WebApi.Models;

namespace Catalog.WebApi.Controllers
{
    /// <summary>
    /// Controller "sottile": traduce HTTP in command/query e delega agli handler.
    /// Nessuna logica di business qui.
    /// </summary>
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private readonly ICommandHandler<CreateProductCommand, Guid> _create;
        private readonly ICommandHandler<ChangeProductPriceCommand> _changePrice;
        private readonly ICommandHandler<DeactivateProductCommand> _deactivate;
        private readonly IQueryHandler<GetProductByIdQuery, ProductDto> _getById;
        private readonly IQueryHandler<ListProductsQuery, PagedResult<ProductDto>> _list;

        public ProductsController(
            ICommandHandler<CreateProductCommand, Guid> create,
            ICommandHandler<ChangeProductPriceCommand> changePrice,
            ICommandHandler<DeactivateProductCommand> deactivate,
            IQueryHandler<GetProductByIdQuery, ProductDto> getById,
            IQueryHandler<ListProductsQuery, PagedResult<ProductDto>> list)
        {
            _create = create;
            _changePrice = changePrice;
            _deactivate = deactivate;
            _getById = getById;
            _list = list;
        }

        // GET api/products?page=1&pageSize=20&includeInactive=false
        [HttpGet, Route("")]
        public async Task<IHttpActionResult> List(CancellationToken cancellationToken, int page = 1, int pageSize = 20, bool includeInactive = false)
        {
            var result = await _list.HandleAsync(new ListProductsQuery(page, pageSize, includeInactive), cancellationToken);
            return Ok(result);
        }

        // GET api/products/{id}
        [HttpGet, Route("{id:guid}", Name = "GetProductById")]
        public async Task<IHttpActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var product = await _getById.HandleAsync(new GetProductByIdQuery(id), cancellationToken);
            return Ok(product);
        }

        // POST api/products
        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
        {
            var id = await _create.HandleAsync(
                new CreateProductCommand(request.Sku, request.Name, request.Price.Value),
                cancellationToken);

            var product = await _getById.HandleAsync(new GetProductByIdQuery(id), cancellationToken);
            return CreatedAtRoute("GetProductById", new { id }, product);
        }

        // PUT api/products/{id}/price
        [HttpPut, Route("{id:guid}/price")]
        public async Task<IHttpActionResult> ChangePrice(Guid id, [FromBody] ChangePriceRequest request, CancellationToken cancellationToken)
        {
            await _changePrice.HandleAsync(new ChangeProductPriceCommand(id, request.Price.Value), cancellationToken);
            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        // DELETE api/products/{id}  (soft delete: il prodotto viene disattivato)
        [HttpDelete, Route("{id:guid}")]
        public async Task<IHttpActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        {
            await _deactivate.HandleAsync(new DeactivateProductCommand(id), cancellationToken);
            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }
    }
}
