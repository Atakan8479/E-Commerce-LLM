namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;

public sealed record SemanticRecommendationCacheEntry(
    string EntryId,
    ReadOnlyMemory<float> Embedding,
    SemanticRecommendationCacheCompatibility Compatibility,
    AlternativeRecommendationResult Result,
    DateTime CreatedAtUtc);