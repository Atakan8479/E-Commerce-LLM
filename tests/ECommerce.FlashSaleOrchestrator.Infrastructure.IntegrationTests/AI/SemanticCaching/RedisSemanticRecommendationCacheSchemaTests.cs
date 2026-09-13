using ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;

namespace ECommerce.FlashSaleOrchestrator
    .Infrastructure.IntegrationTests.AI.SemanticCaching;

public sealed class
    RedisSemanticRecommendationCacheSchemaTests
{
    [Fact]
    public void BuildKey_ShouldCombinePrefixAndEntryId()
    {
        var key =
            RedisSemanticRecommendationCacheSchema.BuildKey(
                "flashsale:semantic-recommendation:",
                "entry-123");

        Assert.Equal(
            "flashsale:semantic-recommendation:entry-123",
            key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildKey_ShouldRejectBlankPrefix(
        string keyPrefix)
    {
        Assert.Throws<ArgumentException>(
            () =>
                RedisSemanticRecommendationCacheSchema
                    .BuildKey(
                        keyPrefix,
                        "entry-123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildKey_ShouldRejectBlankEntryId(
        string entryId)
    {
        Assert.Throws<ArgumentException>(
            () =>
                RedisSemanticRecommendationCacheSchema
                    .BuildKey(
                        "flashsale:semantic-recommendation:",
                        entryId));
    }
}