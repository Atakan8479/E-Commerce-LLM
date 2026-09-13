namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;

public sealed class SemanticRecommendationCacheProfile
{
    public SemanticRecommendationCacheProfile(
        string promptVersion,
        string schemaVersion,
        string semanticCacheVersion,
        string embeddingProfileVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            promptVersion);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            schemaVersion);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            semanticCacheVersion);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            embeddingProfileVersion);

        PromptVersion =
            promptVersion;

        SchemaVersion =
            schemaVersion;

        SemanticCacheVersion =
            semanticCacheVersion;

        EmbeddingProfileVersion =
            embeddingProfileVersion;
    }

    public string PromptVersion { get; }

    public string SchemaVersion { get; }

    public string SemanticCacheVersion { get; }

    public string EmbeddingProfileVersion { get; }

    public SemanticRecommendationCacheCompatibility
        CreateCompatibility(
            string candidateFingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            candidateFingerprint);

        return new SemanticRecommendationCacheCompatibility(
            candidateFingerprint,
            PromptVersion,
            SchemaVersion,
            SemanticCacheVersion,
            EmbeddingProfileVersion);
    }
}