using ECommerce.FlashSaleOrchestrator.Api
    .Contracts.Products;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .Products.GetProduct;
using Microsoft.AspNetCore.Mvc;
using ECommerce.FlashSaleOrchestrator.Api
    .ExceptionHandling;

namespace ECommerce.FlashSaleOrchestrator.Api
    .Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController
    : ControllerBase
{
    private const string ProductNotFoundErrorCode =
        "product-not-found";

    private readonly IQueryHandler<
        GetProductQuery,
        ProductResult?> _getProductHandler;

    public ProductsController(
        IQueryHandler<
            GetProductQuery,
            ProductResult?> getProductHandler)
    {
        _getProductHandler =
            getProductHandler
            ?? throw new ArgumentNullException(
                nameof(getProductHandler));
    }

    [HttpGet("{productId}")]
    [ProducesResponseType(
        typeof(ProductResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>>
        GetByIdAsync(
            Guid productId,
            CancellationToken cancellationToken)
    {
        var result =
            await _getProductHandler.HandleAsync(
                new GetProductQuery(
                    productId),
                cancellationToken);

        if (result is null)
        {
            return ApiProblemDetailsFactory.CreateNotFound(
                HttpContext,
                "Product not found.",
                ProductNotFoundErrorCode,
                $"Product '{productId}' was not found.");
        }

        return Ok(
            new ProductResponse(
                result.ProductId,
                result.Name,
                result.Category));
    }
}