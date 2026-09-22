using ECommerce.FlashSaleOrchestrator.Api
    .Contracts.Carts;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .Carts.GetCart;
using Microsoft.AspNetCore.Mvc;
using ECommerce.FlashSaleOrchestrator.Api
    .ExceptionHandling;

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

    public CartsController(
        IQueryHandler<
            GetCartQuery,
            CartResult?> getCartHandler)
    {
        _getCartHandler =
            getCartHandler
            ?? throw new ArgumentNullException(
                nameof(getCartHandler));
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
            return ApiProblemDetailsFactory.CreateNotFound(
                HttpContext,
                "Cart not found.",
                CartNotFoundErrorCode,
                $"Cart '{cartId}' was not found.");
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
}