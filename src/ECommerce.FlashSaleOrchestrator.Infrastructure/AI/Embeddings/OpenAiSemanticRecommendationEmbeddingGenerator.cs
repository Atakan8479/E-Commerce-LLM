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

    private readonly TimeSpan _requestTimeout;

    public OpenAiSemanticRecommendationEmbeddingGenerator(
        IEmbeddingGenerator<
            string,
            Embedding<float>> embeddingGenerator,
        int expectedDimensions,
        TimeSpan requestTimeout)
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

        if (requestTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestTimeout),
                requestTimeout,
                "Embedding request timeout must be greater than zero.");
        }

        _embeddingGenerator =
            embeddingGenerator;

        _expectedDimensions =
            expectedDimensions;

        _requestTimeout =
            requestTimeout;
    }

    public async Task<SemanticRecommendationEmbedding> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            text);

        cancellationToken.ThrowIfCancellationRequested();

        using var timeoutCancellationTokenSource =
            CancellationTokenSource
                .CreateLinkedTokenSource(
                    cancellationToken);

        timeoutCancellationTokenSource.CancelAfter(
            _requestTimeout);

        Embedding<float> embedding;

        try
        {
            embedding =
                await _embeddingGenerator.GenerateAsync(
                    text,
                    cancellationToken:
                        timeoutCancellationTokenSource.Token);
        }
        catch (OperationCanceledException exception)
            when (
                !cancellationToken.IsCancellationRequested &&
                timeoutCancellationTokenSource
                    .IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Embedding request timed out after " +
                $"{_requestTimeout.TotalSeconds:0.###} seconds.",
                exception);
        }

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