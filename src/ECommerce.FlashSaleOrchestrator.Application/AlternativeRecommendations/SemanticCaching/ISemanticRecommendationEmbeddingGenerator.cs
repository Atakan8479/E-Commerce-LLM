namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;

public interface ISemanticRecommendationEmbeddingGenerator
{
    Task<SemanticRecommendationEmbedding> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default);
}