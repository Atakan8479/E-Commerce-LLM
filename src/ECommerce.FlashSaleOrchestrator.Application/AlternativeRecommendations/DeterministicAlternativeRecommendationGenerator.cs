namespace ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;

public sealed class DeterministicAlternativeRecommendationGenerator
    : IAlternativeRecommendationGenerator
{
    public Task<AlternativeRecommendationResult> GenerateAsync(
        AlternativeRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        cancellationToken.ThrowIfCancellationRequested();

        var recommendations =
            request.Candidates
                .Select(
                    candidate =>
                        new AlternativeRecommendation(
                            candidate.ProductId,
                            "Selected from the validated deterministic candidate set."))
                .ToArray();

        var result =
            new AlternativeRecommendationResult(
                recommendations);

        AlternativeRecommendationResultValidator.Validate(
            request,
            result);

        return Task.FromResult(
            result);
    }
}