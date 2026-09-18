using ECommerce.FlashSaleOrchestrator.Api
    .Contracts.Carts;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Observability;
using ECommerce.FlashSaleOrchestrator.Application
    .Carts.GetCart;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.FlashSaleOrchestrator.Api
    .Controllers;

[ApiController]
[Route("api/carts")]
public sealed class CartsController
    : ControllerBase
{
    private const string CartNotFoundErrorCode =
        "cart-not-found";

    private readonly IQueryHandler<
        GetCartQuery,
        CartResult?> _getCartHandler;

    private readonly ICorrelationContext
        _correlationContext;

    public CartsController(
        IQueryHandler<
            GetCartQuery,
            CartResult?> getCartHandler,
        ICorrelationContext correlationContext)
    {
        _getCartHandler =
            getCartHandler
            ?? throw new ArgumentNullException(
                nameof(getCartHandler));

        _correlationContext =
            correlationContext
            ?? throw new ArgumentNullException(
                nameof(correlationContext));
    }

    [HttpGet("{cartId}")]
    [ProducesResponseType(
        typeof(CartResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartResponse>>
        GetByIdAsync(
            Guid cartId,
            CancellationToken cancellationToken)
    {
        var result =
            await _getCartHandler.HandleAsync(
                new GetCartQuery(
                    cartId),
                cancellationToken);

        if (result is null)
        {
            var problemDetails =
                CreateCartNotFoundProblemDetails(
                    cartId);

            var notFoundResult =
                new NotFoundObjectResult(
                    problemDetails);

            notFoundResult.ContentTypes.Add(
                "application/problem+json");

            return notFoundResult;
        }

        var items =
            result.Items
                .Select(
                    item =>
                        new CartItemResponse(
                            item.ProductId,
                            item.Quantity))
                .ToArray();

        return Ok(
            new CartResponse(
                result.CartId,
                items));
    }

    private ProblemDetails
        CreateCartNotFoundProblemDetails(
            Guid cartId)
    {
        var problemDetails =
            new ProblemDetails
            {
                Status =
                    StatusCodes.Status404NotFound,
                Title =
                    "Cart not found.",
                Detail =
                    $"Cart '{cartId}' was not found.",
                Type =
                    $"urn:flashsale:error:" +
                    CartNotFoundErrorCode,
                Instance =
                    HttpContext.Request.Path
            };

        problemDetails.Extensions[
            "errorCode"] =
            CartNotFoundErrorCode;

        problemDetails.Extensions[
            "correlationId"] =
            _correlationContext.CorrelationId;

        return problemDetails;
    }
}