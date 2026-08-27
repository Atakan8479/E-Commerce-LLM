using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Observability;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Observability;

public sealed class CorrelationContext
    : ICorrelationContext
{
    private string _correlationId =
        Guid.NewGuid().ToString("N");

    public string CorrelationId =>
        _correlationId;

    public void SetCorrelationId(
        string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            correlationId);

        var normalizedCorrelationId =
            correlationId.Trim();

        if (normalizedCorrelationId.Length >
            CorrelationMetadata.MaxLength)
        {
            throw new ArgumentException(
                $"Correlation id cannot exceed " +
                $"{CorrelationMetadata.MaxLength} characters.",
                nameof(correlationId));
        }

        _correlationId =
            normalizedCorrelationId;
    }
}