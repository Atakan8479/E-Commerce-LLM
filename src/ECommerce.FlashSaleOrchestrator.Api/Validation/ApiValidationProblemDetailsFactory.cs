using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Observability;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.FlashSaleOrchestrator.Api
    .Validation;

public static class ApiValidationProblemDetailsFactory
{
    private const string ErrorCode =
        "validation-failed";

    public static IActionResult Create(
        ActionContext actionContext)
    {
        ArgumentNullException.ThrowIfNull(
            actionContext);

        var errors =
            new SortedDictionary<string, string[]>(
                StringComparer.Ordinal);

        foreach (var entry in
                 actionContext.ModelState
                     .Where(
                         entry =>
                             entry.Value is not null &&
                             entry.Value.Errors.Count > 0))
        {
            errors[entry.Key] =
                entry.Value!.Errors
                    .Select(
                        error =>
                            ResolveErrorMessage(
                                error.ErrorMessage))
                    .OrderBy(
                        message =>
                            message,
                        StringComparer.Ordinal)
                    .ToArray();
        }

        var correlationContext =
            actionContext.HttpContext
                .RequestServices
                .GetRequiredService<
                    ICorrelationContext>();

        var problemDetails =
            new ValidationProblemDetails(
                errors)
            {
                Status =
                    StatusCodes.Status400BadRequest,
                Title =
                    "Validation failed.",
                Type =
                    $"urn:flashsale:error:{ErrorCode}",
                Instance =
                    actionContext.HttpContext
                        .Request.Path
            };

        problemDetails.Extensions[
            "errorCode"] =
            ErrorCode;

        problemDetails.Extensions[
            "correlationId"] =
            correlationContext.CorrelationId;

        var result =
            new BadRequestObjectResult(
                problemDetails);

        result.ContentTypes.Add(
            "application/problem+json");

        return result;
    }

    private static string ResolveErrorMessage(
        string errorMessage)
    {
        if (!string.IsNullOrWhiteSpace(
                errorMessage))
        {
            return errorMessage;
        }

        return "The supplied value is invalid.";
    }
}