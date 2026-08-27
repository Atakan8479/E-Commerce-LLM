using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Observability;

namespace ECommerce.FlashSaleOrchestrator.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate
        _next;

    private readonly ILogger<CorrelationIdMiddleware>
        _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(
            next);

        ArgumentNullException.ThrowIfNull(
            logger);

        _next =
            next;

        _logger =
            logger;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        ICorrelationContext correlationContext)
    {
        ArgumentNullException.ThrowIfNull(
            httpContext);

        ArgumentNullException.ThrowIfNull(
            correlationContext);

        var correlationId =
            ResolveCorrelationId(
                httpContext);

        correlationContext.SetCorrelationId(
            correlationId);

        httpContext.Response.Headers[
            CorrelationMetadata.HeaderName] =
            correlationId;

        using (_logger.BeginScope(
                   new Dictionary<string, object>
                   {
                       ["CorrelationId"] =
                           correlationId
                   }))
        {
            await _next(
                httpContext);
        }
    }

    private static string ResolveCorrelationId(
        HttpContext httpContext)
    {
        var incomingCorrelationId =
            httpContext.Request.Headers[
                    CorrelationMetadata.HeaderName]
                .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(
            incomingCorrelationId))
        {
            return Guid.NewGuid()
                .ToString("N");
        }

        var normalizedCorrelationId =
            incomingCorrelationId.Trim();

        if (normalizedCorrelationId.Length >
            CorrelationMetadata.MaxLength)
        {
            return Guid.NewGuid()
                .ToString("N");
        }

        return normalizedCorrelationId;
    }
}