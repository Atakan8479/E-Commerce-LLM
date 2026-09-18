using ECommerce.FlashSaleOrchestrator.Api
    .Contracts.Inventory;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Observability;
using ECommerce.FlashSaleOrchestrator.Application
    .Inventory.DecreaseStock;
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

    private readonly ICommandHandler<
        DecreaseStockCommand,
        DecreaseStockResult> _decreaseStockHandler;

    private readonly ICorrelationContext
        _correlationContext;

    public InventoryController(
        IQueryHandler<
            GetInventoryQuery,
            InventoryResult?> getInventoryHandler,
        ICommandHandler<
            DecreaseStockCommand,
            DecreaseStockResult> decreaseStockHandler,
        ICorrelationContext correlationContext)
    {
        _getInventoryHandler =
            getInventoryHandler
            ?? throw new ArgumentNullException(
                nameof(getInventoryHandler));

        _decreaseStockHandler =
            decreaseStockHandler
            ?? throw new ArgumentNullException(
                nameof(decreaseStockHandler));

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

    [HttpPost("{productId}/decrease")]
    [ProducesResponseType(
        typeof(DecreaseStockResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DecreaseStockResponse>>
        DecreaseStockAsync(
            Guid productId,
            [FromBody] DecreaseStockRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await _decreaseStockHandler.HandleAsync(
                new DecreaseStockCommand(
                    productId,
                    request.Quantity),
                cancellationToken);

        return Ok(
            new DecreaseStockResponse(
                result.ProductId,
                result.RemainingQuantity,
                result.IsDepleted,
                result.IsDepleted,
                _correlationContext.CorrelationId));
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