namespace ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;

public static class AlternativeRecommendationResultValidator
{
    public static void Validate(
        AlternativeRecommendationRequest request,
        AlternativeRecommendationResult result)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(result);

        var candidateIds = request.Candidates
            .Select(candidate => candidate.ProductId)
            .ToHashSet();

        var recommendedProductIds = new HashSet<Guid>();

        foreach (var recommendation in result.Recommendations)
        {
            if (!candidateIds.Contains(recommendation.ProductId))
            {
                throw new AlternativeRecommendationValidationException(
                    $"Recommended product '{recommendation.ProductId}' is not present in the candidate set.");
            }

            if (!recommendedProductIds.Add(recommendation.ProductId))
            {
                throw new AlternativeRecommendationValidationException(
                    $"Product '{recommendation.ProductId}' was recommended more than once.");
            }

            if (string.IsNullOrWhiteSpace(recommendation.Reason))
            {
                throw new AlternativeRecommendationValidationException(
                    $"Recommendation reason for product '{recommendation.ProductId}' cannot be empty.");
            }
        }
    }
}