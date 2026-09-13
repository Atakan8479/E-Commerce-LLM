namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;

public sealed record SemanticRecommendationCacheCompatibility(
    string CandidateFingerprint,
    string PromptVersion,
    string SchemaVersion,
    string SemanticCacheVersion,
    string EmbeddingProfileVersion);