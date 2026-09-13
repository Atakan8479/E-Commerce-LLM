using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.Embeddings;

public static class SemanticRecommendationEmbeddingDependencyInjection
{
    public static IServiceCollection AddSemanticRecommendationEmbedding(
        this IServiceCollection services,
        string modelId,
        Uri endpoint,
        string apiKey,
        int expectedDimensions)
    {
        ArgumentNullException.ThrowIfNull(
            services);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            modelId);

        ArgumentNullException.ThrowIfNull(
            endpoint);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            apiKey);

        if (!endpoint.IsAbsoluteUri ||
            (endpoint.Scheme != Uri.UriSchemeHttp &&
             endpoint.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "Embedding endpoint must be an absolute HTTP or HTTPS URI.",
                nameof(endpoint));
        }

        if (expectedDimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedDimensions),
                expectedDimensions,
                "Expected embedding dimensions must be greater than zero.");
        }

        services.AddSingleton<
            IEmbeddingGenerator<
                string,
                Embedding<float>>>(
            _ =>
            {
                var client =
                    new EmbeddingClient(
                        model: modelId,
                        credential:
                            new ApiKeyCredential(
                                apiKey),
                        options:
                            new OpenAIClientOptions
                            {
                                Endpoint = endpoint
                            });

                return client
                    .AsIEmbeddingGenerator();
            });

        services.AddSingleton<
            ISemanticRecommendationEmbeddingGenerator>(
            serviceProvider =>
                new OpenAiSemanticRecommendationEmbeddingGenerator(
                    serviceProvider.GetRequiredService<
                        IEmbeddingGenerator<
                            string,
                            Embedding<float>>>(),
                    expectedDimensions));

        return services;
    }
}