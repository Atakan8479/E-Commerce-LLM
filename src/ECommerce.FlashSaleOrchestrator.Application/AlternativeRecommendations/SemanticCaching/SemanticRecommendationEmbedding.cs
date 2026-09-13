namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;

public sealed class SemanticRecommendationEmbedding
{
    public SemanticRecommendationEmbedding(
        ReadOnlyMemory<float> vector)
    {
        if (vector.IsEmpty)
        {
            throw new ArgumentException(
                "Embedding vector cannot be empty.",
                nameof(vector));
        }

        foreach (var value in vector.Span)
        {
            if (!float.IsFinite(value))
            {
                throw new ArgumentException(
                    "Embedding vector must contain only finite values.",
                    nameof(vector));
            }
        }

        Vector = vector;
    }

    public ReadOnlyMemory<float> Vector { get; }
}