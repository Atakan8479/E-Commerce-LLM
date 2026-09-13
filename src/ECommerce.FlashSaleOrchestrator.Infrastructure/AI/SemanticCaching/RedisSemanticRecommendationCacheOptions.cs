namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;

internal sealed class
    RedisSemanticRecommendationCacheOptions
{
    public RedisSemanticRecommendationCacheOptions(
        string indexName,
        string keyPrefix,
        int vectorDimensions,
        double similarityThreshold,
        TimeSpan entryTimeToLive)
    {
        if (string.IsNullOrWhiteSpace(
                indexName))
        {
            throw new ArgumentException(
                "Redis semantic cache index name is required.",
                nameof(indexName));
        }

        if (string.IsNullOrWhiteSpace(
                keyPrefix))
        {
            throw new ArgumentException(
                "Redis semantic cache key prefix is required.",
                nameof(keyPrefix));
        }

        if (vectorDimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vectorDimensions),
                "Vector dimensions must be greater than zero.");
        }

        if (similarityThreshold is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(similarityThreshold),
                "Similarity threshold must be between 0 and 1.");
        }

        if (entryTimeToLive <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entryTimeToLive),
                "Cache entry TTL must be greater than zero.");
        }

        IndexName =
            indexName.Trim();

        KeyPrefix =
            keyPrefix.Trim();

        VectorDimensions =
            vectorDimensions;

        SimilarityThreshold =
            similarityThreshold;

        EntryTimeToLive =
            entryTimeToLive;
    }

    public string IndexName { get; }

    public string KeyPrefix { get; }

    public int VectorDimensions { get; }

    public double SimilarityThreshold { get; }

    public TimeSpan EntryTimeToLive { get; }
}