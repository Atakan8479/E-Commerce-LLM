namespace ECommerce.FlashSaleOrchestrator.Infrastructure.AI;

internal sealed class AlternativeRecommendationAiOptions
{
    public AlternativeRecommendationAiOptions(
        TimeSpan requestTimeout)
    {
        if (requestTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestTimeout),
                requestTimeout,
                "AI request timeout must be greater than zero.");
        }

        RequestTimeout =
            requestTimeout;
    }

    public TimeSpan RequestTimeout { get; }
}