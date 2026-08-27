namespace ECommerce.FlashSaleOrchestrator.Application.Abstractions.Observability;

public static class CorrelationMetadata
{
    public const string HeaderName =
        "X-Correlation-ID";

    public const int MaxLength =
        128;
}