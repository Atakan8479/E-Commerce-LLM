using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.AI;

public static class
    AlternativeRecommendationAiDependencyInjection
{
    public static IServiceCollection
        AddAlternativeRecommendationAi(
            this IServiceCollection services,
            string modelId,
            Uri endpoint,
            string apiKey)
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
                "The AI endpoint must be an absolute " +
                "HTTP or HTTPS URI.",
                nameof(endpoint));
        }

#pragma warning disable SKEXP0010
        services.AddOpenAIChatCompletion(
            modelId: modelId,
            endpoint: endpoint,
            apiKey: apiKey);
#pragma warning restore SKEXP0010

        services.AddScoped<
            SemanticKernelAlternativeRecommendationGenerator>();

        services.AddScoped<
            IUncachedAlternativeRecommendationGenerator>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    SemanticKernelAlternativeRecommendationGenerator>());

        services.AddScoped<
            CachedSemanticAlternativeRecommendationGenerator>();

        services.AddScoped<
            DeterministicAlternativeRecommendationGenerator>();

        services.AddScoped<
            IAlternativeRecommendationGenerator,
            ResilientAlternativeRecommendationGenerator>();

        services.AddScoped<
            IAlternativeRecommendationExecutor>(
            serviceProvider =>
                (IAlternativeRecommendationExecutor)
                serviceProvider.GetRequiredService<
                    IAlternativeRecommendationGenerator>());

        return services;
    }
}