namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;

internal static class
    RedisSemanticRecommendationCacheSchema
{
    public const string EntryId =
        "entryId";

    public const string CandidateFingerprint =
        "candidateFingerprint";

    public const string PromptVersion =
        "promptVersion";

    public const string SchemaVersion =
        "schemaVersion";

    public const string SemanticCacheVersion =
        "semanticCacheVersion";

    public const string EmbeddingProfileVersion =
        "embeddingProfileVersion";

    public const string ResultJson =
        "resultJson";

    public const string CreatedAtUtc =
        "createdAtUtc";

    public const string Embedding =
        "embedding";

    public const string Distance =
        "distance";

    public static string BuildKey(
        string keyPrefix,
        string entryId)
    {
        if (string.IsNullOrWhiteSpace(
                keyPrefix))
        {
            throw new ArgumentException(
                "Redis semantic cache key prefix is required.",
                nameof(keyPrefix));
        }

        if (string.IsNullOrWhiteSpace(
                entryId))
        {
            throw new ArgumentException(
                "Redis semantic cache entry id is required.",
                nameof(entryId));
        }

        return string.Concat(
            keyPrefix.Trim(),
            entryId.Trim());
    }
}