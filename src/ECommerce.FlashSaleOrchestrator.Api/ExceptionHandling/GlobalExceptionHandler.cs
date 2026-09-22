using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Observability;
using ECommerce.FlashSaleOrchestrator.Application
    .Inventory.DecreaseStock;
using ECommerce.FlashSaleOrchestrator.Domain
    .Inventory.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.FlashSaleOrchestrator.Api
    .ExceptionHandling;

public sealed class GlobalExceptionHandler
    : IExceptionHandler
{
    private readonly IProblemDetailsService
        _problemDetailsService;

    private readonly ILogger<GlobalExceptionHandler>
        _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService =
            problemDetailsService
            ?? throw new ArgumentNullException(
                nameof(problemDetailsService));

        _logger =
            logger
            ?? throw new ArgumentNullException(
                nameof(logger));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            httpContext);

        ArgumentNullException.ThrowIfNull(
            exception);

        var error =
            MapException(
                exception);

        if (error.StatusCode >=
            StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception while processing HTTP request.");
        }
        else
        {
            _logger.LogInformation(
                exception,
                "HTTP request failed with status code {StatusCode}.",
                error.StatusCode);
        }

        var correlationContext =
            httpContext.RequestServices
                .GetRequiredService<
                    ICorrelationContext>();

        var problemDetails =
            new ProblemDetails
            {
                Status =
                    error.StatusCode,
                Title =
                    error.Title,
                Detail =
                    error.Detail,
                Type =
                    $"urn:flashsale:error:{error.ErrorCode}",
                Instance =
                    httpContext.Request.Path
            };

        problemDetails.Extensions[
            "errorCode"] =
            error.ErrorCode;

        problemDetails.Extensions[
            "correlationId"] =
            correlationContext.CorrelationId;

        httpContext.Response.StatusCode =
            error.StatusCode;

        await _problemDetailsService.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext =
                    httpContext,
                ProblemDetails =
                    problemDetails
            });

        return true;
    }

    private static ErrorDescriptor MapException(
        Exception exception)
    {
        return exception switch
        {
            InventoryItemNotFoundException =>
                new ErrorDescriptor(
                    StatusCodes.Status404NotFound,
                    "Inventory item not found.",
                    "inventory-item-not-found",
                    "The requested inventory item was not found."),

            InventoryConcurrencyException =>
                new ErrorDescriptor(
                    StatusCodes.Status409Conflict,
                    "Inventory update conflict.",
                    "inventory-concurrency-conflict",
                    "Inventory changed while this request was being processed. " +
                    "Refresh the current inventory state and retry if appropriate."),

            InsufficientStockException =>
                new ErrorDescriptor(
                    StatusCodes.Status409Conflict,
                    "Insufficient stock.",
                    "insufficient-stock",
                    "The requested quantity is not available."),

            ArgumentOutOfRangeException =>
                new ErrorDescriptor(
                    StatusCodes.Status400BadRequest,
                    "Invalid request.",
                    "invalid-request",
                    "The request contains an invalid value."),

            ArgumentException
                when exception is not
                    ArgumentNullException =>
                new ErrorDescriptor(
                    StatusCodes.Status400BadRequest,
                    "Invalid request.",
                    "invalid-request",
                    "The request contains an invalid value."),

            _ =>
                new ErrorDescriptor(
                    StatusCodes
                        .Status500InternalServerError,
                    "An unexpected error occurred.",
                    "internal-server-error",
                    "An unexpected error occurred while " +
                    "processing the request.")
        };
    }

    private sealed record ErrorDescriptor(
        int StatusCode,
        string Title,
        string ErrorCode,
        string Detail);
}