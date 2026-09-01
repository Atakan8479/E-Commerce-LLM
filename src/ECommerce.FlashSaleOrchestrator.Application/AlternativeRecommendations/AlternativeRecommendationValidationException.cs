using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Resilience;

namespace ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;

public sealed class AlternativeRecommendationValidationException
    : Exception,
      INonRetryableException
{
    public AlternativeRecommendationValidationException(
        string message)
        : base(message)
    {
    }

    public AlternativeRecommendationValidationException(
        string message,
        Exception innerException)
        : base(
            message,
            innerException)
    {
    }
}