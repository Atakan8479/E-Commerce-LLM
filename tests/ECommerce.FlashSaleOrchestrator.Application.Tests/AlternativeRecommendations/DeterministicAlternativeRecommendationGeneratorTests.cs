using ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;

namespace ECommerce.FlashSaleOrchestrator.Application.Tests.AlternativeRecommendations;

public sealed class DeterministicAlternativeRecommendationGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_ShouldReturnCandidatesInInputOrder()
    {
        var firstCandidateId =
            Guid.NewGuid();

        var secondCandidateId =
            Guid.NewGuid();

        var request =
            new AlternativeRecommendationRequest(
                "deterministic-fallback-correlation",
                new DepletedProductContext(
                    Guid.NewGuid(),
                    "Depleted Product",
                    "Electronics"),
                [
                    new AlternativeCandidate(
                        firstCandidateId,
                        "Candidate A",
                        "Electronics",
                        10),
                    new AlternativeCandidate(
                        secondCandidateId,
                        "Candidate B",
                        "Electronics",
                        5)
                ]);

        var generator =
            new DeterministicAlternativeRecommendationGenerator();

        var result =
            await generator.GenerateAsync(
                request);

        Assert.Equal(
            2,
            result.Recommendations.Count);

        Assert.Equal(
            firstCandidateId,
            result.Recommendations[0].ProductId);

        Assert.Equal(
            secondCandidateId,
            result.Recommendations[1].ProductId);

        Assert.All(
            result.Recommendations,
            recommendation =>
                Assert.False(
                    string.IsNullOrWhiteSpace(
                        recommendation.Reason)));
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturnEmpty_WhenNoCandidatesExist()
    {
        var request =
            new AlternativeRecommendationRequest(
                "deterministic-empty-correlation",
                new DepletedProductContext(
                    Guid.NewGuid(),
                    "Depleted Product",
                    "Electronics"),
                []);

        var generator =
            new DeterministicAlternativeRecommendationGenerator();

        var result =
            await generator.GenerateAsync(
                request);

        Assert.Empty(
            result.Recommendations);
    }

    [Fact]
    public async Task GenerateAsync_ShouldRespectCancellation()
    {
        var request =
            new AlternativeRecommendationRequest(
                "deterministic-cancellation-correlation",
                new DepletedProductContext(
                    Guid.NewGuid(),
                    "Depleted Product",
                    "Electronics"),
                [
                    new AlternativeCandidate(
                        Guid.NewGuid(),
                        "Candidate A",
                        "Electronics",
                        10)
                ]);

        var generator =
            new DeterministicAlternativeRecommendationGenerator();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            () =>
                generator.GenerateAsync(
                    request,
                    cancellationTokenSource.Token));
    }
}