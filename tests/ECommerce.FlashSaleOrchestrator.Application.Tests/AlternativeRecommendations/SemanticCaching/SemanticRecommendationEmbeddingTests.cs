using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;

namespace ECommerce.FlashSaleOrchestrator.Application.Tests
    .AlternativeRecommendations.SemanticCaching;

public sealed class SemanticRecommendationEmbeddingTests
{
    [Fact]
    public void Constructor_ShouldAcceptFiniteVector()
    {
        float[] vector =
        [
            0.1f,
            -0.2f,
            0.3f
        ];

        var embedding =
            new SemanticRecommendationEmbedding(
                vector);

        Assert.Equal(
            vector,
            embedding.Vector.ToArray());
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyVector()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new SemanticRecommendationEmbedding(
                    ReadOnlyMemory<float>.Empty));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void Constructor_ShouldRejectNonFiniteValues(
        float invalidValue)
    {
        float[] vector =
        [
            0.1f,
            invalidValue,
            0.3f
        ];

        Assert.Throws<ArgumentException>(
            () =>
                new SemanticRecommendationEmbedding(
                    vector));
    }
}