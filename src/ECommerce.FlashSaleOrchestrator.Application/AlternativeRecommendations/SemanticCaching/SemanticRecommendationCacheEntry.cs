namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;

public sealed record SemanticRecommendationCacheEntry(
    string EntryId,
    SemanticRecommendationEmbedding Embedding,
    SemanticRecommendationCacheCompatibility Compatibility,
    AlternativeRecommendationResult Result,
    DateTime CreatedAtUtc);