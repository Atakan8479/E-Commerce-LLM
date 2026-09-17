namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;

public sealed record AlternativeRecommendationGenerationOutcome
{
    public AlternativeRecommendationGenerationOutcome(
        AlternativeRecommendationResult result,
        AlternativeRecommendationSource source)
    {
        ArgumentNullException.ThrowIfNull(
            result);

        if (!Enum.IsDefined(source))
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Recommendation source is not supported.");
        }

        Result = result;
        Source = source;
    }

    public AlternativeRecommendationResult Result { get; }

    public AlternativeRecommendationSource Source { get; }
}