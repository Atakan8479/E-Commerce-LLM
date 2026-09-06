using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;

namespace ECommerce.FlashSaleOrchestrator.Application.Tests.AlternativeRecommendations;

public sealed class AlternativeRecommendationResultValidatorTests
{
    [Fact]
    public void Validate_ShouldSucceed_WhenRecommendationsUseOnlyCandidateProductIds()
    {
        var candidateProductId = Guid.NewGuid();

        var request = CreateRequest(
            new AlternativeCandidate(
                candidateProductId,
                "Razer Viper",
                "mouse",
                20));

        var result = new AlternativeRecommendationResult(
        [
            new AlternativeRecommendation(
                candidateProductId,
                "Suitable gaming mouse alternative.")
        ]);

        AlternativeRecommendationResultValidator.Validate(request, result);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenRecommendationProductIdIsOutsideCandidateSet()
    {
        var candidateProductId = Guid.NewGuid();
        var hallucinatedProductId = Guid.NewGuid();

        var request = CreateRequest(
            new AlternativeCandidate(
                candidateProductId,
                "Razer Viper",
                "mouse",
                20));

        var result = new AlternativeRecommendationResult(
        [
            new AlternativeRecommendation(
                hallucinatedProductId,
                "Suitable alternative.")
        ]);

        var exception = Assert.Throws<AlternativeRecommendationValidationException>(
            () => AlternativeRecommendationResultValidator.Validate(request, result));

        Assert.Contains(
            hallucinatedProductId.ToString(),
            exception.Message);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenSameProductIsRecommendedMoreThanOnce()
    {
        var candidateProductId = Guid.NewGuid();

        var request = CreateRequest(
            new AlternativeCandidate(
                candidateProductId,
                "Razer Viper",
                "mouse",
                20));

        var result = new AlternativeRecommendationResult(
        [
            new AlternativeRecommendation(
                candidateProductId,
                "Suitable gaming alternative."),
            new AlternativeRecommendation(
                candidateProductId,
                "Another reason for the same product.")
        ]);

        Assert.Throws<AlternativeRecommendationValidationException>(
            () => AlternativeRecommendationResultValidator.Validate(request, result));
    }

    [Fact]
    public void Validate_ShouldThrow_WhenRecommendationReasonIsEmpty()
    {
        var candidateProductId = Guid.NewGuid();

        var request = CreateRequest(
            new AlternativeCandidate(
                candidateProductId,
                "Razer Viper",
                "mouse",
                20));

        var result = new AlternativeRecommendationResult(
        [
            new AlternativeRecommendation(
                candidateProductId,
                string.Empty)
        ]);

        Assert.Throws<AlternativeRecommendationValidationException>(
            () => AlternativeRecommendationResultValidator.Validate(request, result));
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenRecommendationsAreEmpty()
    {
        var candidateProductId = Guid.NewGuid();

        var request = CreateRequest(
            new AlternativeCandidate(
                candidateProductId,
                "Razer Viper",
                "mouse",
                20));

        var result = new AlternativeRecommendationResult([]);

        AlternativeRecommendationResultValidator.Validate(request, result);
    }

    private static AlternativeRecommendationRequest CreateRequest(
        params AlternativeCandidate[] candidates)
    {
        return new AlternativeRecommendationRequest(
            $"correlation-{Guid.NewGuid():N}",
            new DepletedProductContext(
                Guid.NewGuid(),
                "Logitech Gaming Mouse",
                "mouse"),
            candidates);
    }
}