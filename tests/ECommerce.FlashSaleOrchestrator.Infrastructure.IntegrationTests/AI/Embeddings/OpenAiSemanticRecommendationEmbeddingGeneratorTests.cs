using ECommerce.FlashSaleOrchestrator.Infrastructure
   .AI.Embeddings;
using Microsoft.Extensions.AI;

namespace ECommerce.FlashSaleOrchestrator
    .Infrastructure.IntegrationTests.AI.Embeddings;

public sealed class OpenAiSemanticRecommendationEmbeddingGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_ShouldReturnProviderVector()
    {
        var fakeGenerator =
            new FakeEmbeddingGenerator(
                [
                    0.1f,
                    -0.2f,
                    0.3f
                ]);

        var generator =
            CreateGenerator(
                fakeGenerator);

        var result =
            await generator.GenerateAsync(
                "gaming mouse");

        Assert.Equal(
            new[]
            {
                0.1f,
                -0.2f,
                0.3f
            },
            result.Vector.ToArray());

        Assert.Equal(
            "gaming mouse",
            fakeGenerator.LastInput);
    }

    [Fact]
    public async Task GenerateAsync_ShouldRejectUnexpectedDimensions()
    {
        var fakeGenerator =
            new FakeEmbeddingGenerator(
                [
                    0.1f,
                    0.2f
                ]);

        var generator =
            CreateGenerator(
                fakeGenerator);

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    generator.GenerateAsync(
                        "gaming mouse"));

        Assert.Contains(
            "expected 3",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_ShouldRejectBlankText()
    {
        var fakeGenerator =
            new FakeEmbeddingGenerator(
                [
                    0.1f,
                    0.2f,
                    0.3f
                ]);

        var generator =
            CreateGenerator(
                fakeGenerator);

        await Assert.ThrowsAsync<
            ArgumentException>(
            () =>
                generator.GenerateAsync(
                    "   "));

        Assert.Null(
            fakeGenerator.LastInput);
    }

    [Fact]
    public async Task GenerateAsync_ShouldPropagateCallerCancellation()
    {
        var fakeGenerator =
            new FakeEmbeddingGenerator(
                [
                    0.1f,
                    0.2f,
                    0.3f
                ]);

        var generator =
            CreateGenerator(
                fakeGenerator);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            () =>
                generator.GenerateAsync(
                    "gaming mouse",
                    cancellationTokenSource.Token));

        Assert.Null(
            fakeGenerator.LastInput);
    }

    [Fact]
    public async Task GenerateAsync_ShouldThrowTimeoutException_WhenProviderExceedsTimeout()
    {
        var blockingGenerator =
            new BlockingEmbeddingGenerator();

        var generator =
            new OpenAiSemanticRecommendationEmbeddingGenerator(
                blockingGenerator,
                expectedDimensions: 3,
                requestTimeout:
                    TimeSpan.FromMilliseconds(
                        100));

        var exception =
            await Assert.ThrowsAsync<
                TimeoutException>(
                () =>
                    generator.GenerateAsync(
                        "gaming mouse"));

        Assert.True(
            blockingGenerator.WasCalled);

        Assert.Contains(
            "timed out",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_ShouldRejectNonPositiveRequestTimeout()
    {
        var fakeGenerator =
            new FakeEmbeddingGenerator(
                [
                    0.1f,
                    0.2f,
                    0.3f
                ]);

        Assert.Throws<
            ArgumentOutOfRangeException>(
            () =>
                new OpenAiSemanticRecommendationEmbeddingGenerator(
                    fakeGenerator,
                    expectedDimensions: 3,
                    requestTimeout:
                        TimeSpan.Zero));
    }

    private static
        OpenAiSemanticRecommendationEmbeddingGenerator
        CreateGenerator(
            IEmbeddingGenerator<
                string,
                Embedding<float>> embeddingGenerator)
    {
        return new OpenAiSemanticRecommendationEmbeddingGenerator(
            embeddingGenerator,
            expectedDimensions: 3,
            requestTimeout:
                TimeSpan.FromSeconds(
                    5));
    }

    private sealed class FakeEmbeddingGenerator
        : IEmbeddingGenerator<
            string,
            Embedding<float>>
    {
        private readonly float[] _vector;

        public FakeEmbeddingGenerator(
            float[] vector)
        {
            _vector =
                vector;
        }

        public string? LastInput { get; private set; }

        public Task<
            GeneratedEmbeddings<
                Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LastInput =
                values.Single();

            GeneratedEmbeddings<
                Embedding<float>> result =
                [
                    new Embedding<float>(
                        _vector)
                ];

            return Task.FromResult(
                result);
        }

        public object? GetService(
            Type serviceType,
            object? serviceKey)
        {
            if (serviceKey is not null)
            {
                return null;
            }

            return serviceType.IsInstanceOfType(
                this)
                ? this
                : null;
        }

        public void Dispose()
        {
        }
    }

    private sealed class BlockingEmbeddingGenerator
        : IEmbeddingGenerator<
            string,
            Embedding<float>>
    {
        public bool WasCalled { get; private set; }

        public async Task<
            GeneratedEmbeddings<
                Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            _ =
                values.Single();

            WasCalled =
                true;

            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                cancellationToken);

            throw new InvalidOperationException(
                "Blocking embedding generator unexpectedly completed.");
        }

        public object? GetService(
            Type serviceType,
            object? serviceKey)
        {
            if (serviceKey is not null)
            {
                return null;
            }

            return serviceType.IsInstanceOfType(
                this)
                ? this
                : null;
        }

        public void Dispose()
        {
        }
    }
}