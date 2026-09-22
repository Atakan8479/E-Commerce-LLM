using ECommerce.FlashSaleOrchestrator.Api
    .Contracts.Catalog;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .Catalog.GetCatalog;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.FlashSaleOrchestrator.Api
    .Controllers;

[ApiController]
[Route("api/catalog")]
public sealed class CatalogController
    : ControllerBase
{
    private readonly IQueryHandler<
        GetCatalogQuery,
        IReadOnlyList<CatalogItemResult>>
        _getCatalogHandler;

    public CatalogController(
        IQueryHandler<
            GetCatalogQuery,
            IReadOnlyList<CatalogItemResult>>
            getCatalogHandler)
    {
        _getCatalogHandler =
            getCatalogHandler
            ?? throw new ArgumentNullException(
                nameof(getCatalogHandler));
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(CatalogResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogResponse>>
        GetAsync(
            CancellationToken cancellationToken)
    {
        var items =
            await _getCatalogHandler.HandleAsync(
                new GetCatalogQuery(),
                cancellationToken);

        return Ok(
            new CatalogResponse(
                items
                    .Select(
                        item =>
                            new CatalogItemResponse(
                                item.ProductId,
                                item.Name,
                                item.Category,
                                item.AvailableQuantity,
                                item.IsDepleted))
                    .ToArray()));
    }
}