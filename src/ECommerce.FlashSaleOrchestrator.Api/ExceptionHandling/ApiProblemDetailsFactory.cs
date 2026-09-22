using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Observability;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.FlashSaleOrchestrator.Api
    .ExceptionHandling;

public static class ApiProblemDetailsFactory
{
    public static NotFoundObjectResult CreateNotFound(
        HttpContext httpContext,
        string title,
        string errorCode,
        string detail)
    {
        ArgumentNullException.ThrowIfNull(
            httpContext);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            title);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            errorCode);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            detail);

        var correlationContext =
            httpContext.RequestServices
                .GetRequiredService<
                    ICorrelationContext>();

        var problemDetails =
            new ProblemDetails
            {
                Status =
                    StatusCodes.Status404NotFound,
                Title =
                    title,
                Detail =
                    detail,
                Type =
                    $"urn:flashsale:error:{errorCode}",
                Instance =
                    httpContext.Request.Path
            };

        problemDetails.Extensions[
            "errorCode"] =
            errorCode;

        problemDetails.Extensions[
            "correlationId"] =
            correlationContext.CorrelationId;

        var result =
            new NotFoundObjectResult(
                problemDetails);

        result.ContentTypes.Add(
            "application/problem+json");

        return result;
    }
}