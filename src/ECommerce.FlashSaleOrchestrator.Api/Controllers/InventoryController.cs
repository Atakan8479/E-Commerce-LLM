using ECommerce.FlashSaleOrchestrator.Api
    .Contracts.Inventory;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Observability;
using ECommerce.FlashSaleOrchestrator.Application
    .Inventory.GetInventory;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.FlashSaleOrchestrator.Api
    .Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryController
    : ControllerBase
{
    private const string InventoryItemNotFoundErrorCode =
        "inventory-item-not-found";

    private readonly IQueryHandler<
        GetInventoryQuery,
        InventoryResult?> _getInventoryHandler;

    private readonly ICorrelationContext
        _correlationContext;

    public InventoryController(
        IQueryHandler<
            GetInventoryQuery,
            InventoryResult?> getInventoryHandler,
        ICorrelationContext correlationContext)
    {
        _getInventoryHandler =
            getInventoryHandler
            ?? throw new ArgumentNullException(
                nameof(getInventoryHandler));

        _correlationContext =
            correlationContext
            ?? throw new ArgumentNullException(
                nameof(correlationContext));
    }

    [HttpGet("{productId}")]
    [ProducesResponseType(
        typeof(InventoryResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryResponse>>
        GetByProductIdAsync(
            Guid productId,
            CancellationToken cancellationToken)
    {
        var result =
            await _getInventoryHandler.HandleAsync(
                new GetInventoryQuery(
                    productId),
                cancellationToken);

        if (result is null)
        {
            var problemDetails =
                CreateInventoryItemNotFoundProblemDetails(
                    productId);

            var notFoundResult =
                new NotFoundObjectResult(
                    problemDetails);

            notFoundResult.ContentTypes.Add(
                "application/problem+json");

            return notFoundResult;
        }

        return Ok(
            new InventoryResponse(
                result.ProductId,
                result.AvailableQuantity,
                result.IsDepleted));
    }

    private ProblemDetails
        CreateInventoryItemNotFoundProblemDetails(
            Guid productId)
    {
        var problemDetails =
            new ProblemDetails
            {
                Status =
                    StatusCodes.Status404NotFound,
                Title =
                    "Inventory item not found.",
                Detail =
                    $"Inventory item for product " +
                    $"'{productId}' was not found.",
                Type =
                    $"urn:flashsale:error:" +
                    InventoryItemNotFoundErrorCode,
                Instance =
                    HttpContext.Request.Path
            };

        problemDetails.Extensions[
            "errorCode"] =
            InventoryItemNotFoundErrorCode;

        problemDetails.Extensions[
            "correlationId"] =
            _correlationContext.CorrelationId;

        return problemDetails;
    }
}