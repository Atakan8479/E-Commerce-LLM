using ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;

namespace ECommerce.FlashSaleOrchestrator
    .Infrastructure.IntegrationTests.AI.SemanticCaching;

public sealed class
    RedisSemanticRecommendationCacheOptionsTests
{
    [Fact]
    public void Constructor_ShouldCreateOptions_WhenValuesAreValid()
    {
        var options =
            new RedisSemanticRecommendationCacheOptions(
                "flashsale:semantic-recommendations:idx",
                "flashsale:semantic-recommendation:",
                1024,
                0.90,
                TimeSpan.FromHours(6));

        Assert.Equal(
            "flashsale:semantic-recommendations:idx",
            options.IndexName);

        Assert.Equal(
            "flashsale:semantic-recommendation:",
            options.KeyPrefix);

        Assert.Equal(
            1024,
            options.VectorDimensions);

        Assert.Equal(
            0.90,
            options.SimilarityThreshold);

        Assert.Equal(
            TimeSpan.FromHours(6),
            options.EntryTimeToLive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldRejectInvalidVectorDimensions(
        int vectorDimensions)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new RedisSemanticRecommendationCacheOptions(
                    "flashsale:semantic-recommendations:idx",
                    "flashsale:semantic-recommendation:",
                    vectorDimensions,
                    0.90,
                    TimeSpan.FromHours(6)));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Constructor_ShouldRejectInvalidSimilarityThreshold(
        double similarityThreshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new RedisSemanticRecommendationCacheOptions(
                    "flashsale:semantic-recommendations:idx",
                    "flashsale:semantic-recommendation:",
                    1024,
                    similarityThreshold,
                    TimeSpan.FromHours(6)));
    }

    [Fact]
    public void Constructor_ShouldRejectNonPositiveTimeToLive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new RedisSemanticRecommendationCacheOptions(
                    "flashsale:semantic-recommendations:idx",
                    "flashsale:semantic-recommendation:",
                    1024,
                    0.90,
                    TimeSpan.Zero));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldRejectBlankIndexName(
        string indexName)
    {
        Assert.Throws<ArgumentException>(
            () =>
                new RedisSemanticRecommendationCacheOptions(
                    indexName,
                    "flashsale:semantic-recommendation:",
                    1024,
                    0.90,
                    TimeSpan.FromHours(6)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldRejectBlankKeyPrefix(
        string keyPrefix)
    {
        Assert.Throws<ArgumentException>(
            () =>
                new RedisSemanticRecommendationCacheOptions(
                    "flashsale:semantic-recommendations:idx",
                    keyPrefix,
                    1024,
                    0.90,
                    TimeSpan.FromHours(6)));
    }
}