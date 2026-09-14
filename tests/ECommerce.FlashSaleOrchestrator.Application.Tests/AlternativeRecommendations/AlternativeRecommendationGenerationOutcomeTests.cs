using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;

namespace ECommerce.FlashSaleOrchestrator.Application.Tests
    .AlternativeRecommendations;

public sealed class
    AlternativeRecommendationGenerationOutcomeTests
{
    [Fact]
    public void Constructor_ShouldPreserveResultAndSource()
    {
        var result =
            new AlternativeRecommendationResult(
                []);

        var outcome =
            new AlternativeRecommendationGenerationOutcome(
                result,
                AlternativeRecommendationSource.Cache);

        Assert.Same(
            result,
            outcome.Result);

        Assert.Equal(
            AlternativeRecommendationSource.Cache,
            outcome.Source);
    }

    [Fact]
    public void Constructor_ShouldRejectNullResult()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
                new AlternativeRecommendationGenerationOutcome(
                    null!,
                    AlternativeRecommendationSource.Llm));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Constructor_ShouldRejectUnsupportedSource(
        int sourceValue)
    {
        var result =
            new AlternativeRecommendationResult(
                []);

        var source =
            (AlternativeRecommendationSource)
            sourceValue;

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new AlternativeRecommendationGenerationOutcome(
                    result,
                    source));
    }
}