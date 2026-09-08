using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;
using Microsoft.Extensions.AI;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.Embeddings;

internal sealed class OpenAiSemanticRecommendationEmbeddingGenerator
    : ISemanticRecommendationEmbeddingGenerator
{
    private readonly IEmbeddingGenerator<
        string,
        Embedding<float>> _embeddingGenerator;

    private readonly int _expectedDimensions;

    public OpenAiSemanticRecommendationEmbeddingGenerator(
        IEmbeddingGenerator<
            string,
            Embedding<float>> embeddingGenerator,
        int expectedDimensions)
    {
        ArgumentNullException.ThrowIfNull(
            embeddingGenerator);

        if (expectedDimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedDimensions),
                expectedDimensions,
                "Expected embedding dimensions must be greater than zero.");
        }

        _embeddingGenerator =
            embeddingGenerator;

        _expectedDimensions =
            expectedDimensions;
    }

    public async Task<SemanticRecommendationEmbedding> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            text);

        cancellationToken.ThrowIfCancellationRequested();

        var embedding =
            await _embeddingGenerator.GenerateAsync(
                text,
                cancellationToken:
                    cancellationToken);

        if (embedding.Vector.Length !=
            _expectedDimensions)
        {
            throw new InvalidOperationException(
                $"Embedding provider returned " +
                $"{embedding.Vector.Length} dimensions; " +
                $"expected {_expectedDimensions}.");
        }

        return new SemanticRecommendationEmbedding(
            embedding.Vector);
    }
}