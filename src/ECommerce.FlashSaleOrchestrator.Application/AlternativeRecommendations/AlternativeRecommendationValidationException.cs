namespace ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;

public sealed class AlternativeRecommendationValidationException : Exception
{
    public AlternativeRecommendationValidationException(string message)
        : base(message)
    {
    }

    public AlternativeRecommendationValidationException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}