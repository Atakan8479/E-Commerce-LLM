using ECommerce.FlashSaleOrchestrator.Api
    .Contracts.Products;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Observability;
using ECommerce.FlashSaleOrchestrator.Application
    .Products.GetProduct;
using Microsoft.AspNetCore.Mvc;

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

    private readonly ICorrelationContext
        _correlationContext;

    public ProductsController(
        IQueryHandler<
            GetProductQuery,
            ProductResult?> getProductHandler,
        ICorrelationContext correlationContext)
    {
        _getProductHandler =
            getProductHandler
            ?? throw new ArgumentNullException(
                nameof(getProductHandler));

        _correlationContext =
            correlationContext
            ?? throw new ArgumentNullException(
                nameof(correlationContext));
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
            var problemDetails =
                CreateProductNotFoundProblemDetails(
                    productId);

            var notFoundResult =
                new NotFoundObjectResult(
                    problemDetails);

            notFoundResult.ContentTypes.Add(
                "application/problem+json");

            return notFoundResult;
        }

        return Ok(
            new ProductResponse(
                result.ProductId,
                result.Name,
                result.Category));
    }

    private ProblemDetails
        CreateProductNotFoundProblemDetails(
            Guid productId)
    {
        var problemDetails =
            new ProblemDetails
            {
                Status =
                    StatusCodes.Status404NotFound,
                Title =
                    "Product not found.",
                Detail =
                    $"Product '{productId}' was not found.",
                Type =
                    $"urn:flashsale:error:" +
                    ProductNotFoundErrorCode,
                Instance =
                    HttpContext.Request.Path
            };

        problemDetails.Extensions[
            "errorCode"] =
            ProductNotFoundErrorCode;

        problemDetails.Extensions[
            "correlationId"] =
            _correlationContext.CorrelationId;

        return problemDetails;
    }
}