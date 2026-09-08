using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;

internal static class
    RedisFloat32VectorSerializer
{
    public static byte[] Serialize(
        SemanticRecommendationEmbedding embedding,
        int expectedDimensions)
    {
        ArgumentNullException.ThrowIfNull(
            embedding);

        if (expectedDimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedDimensions),
                "Expected dimensions must be greater than zero.");
        }

        var vector =
            embedding.Vector;

        if (vector.Length != expectedDimensions)
        {
            throw new InvalidOperationException(
                $"Embedding dimension mismatch. " +
                $"Expected {expectedDimensions}, " +
                $"received {vector.Length}.");
        }

        var values =
            vector.ToArray();

        var bytes =
            new byte[
                values.Length *
                sizeof(float)];

        Buffer.BlockCopy(
            values,
            0,
            bytes,
            0,
            bytes.Length);

        return bytes;
    }
}