using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.AI;

public static class AlternativeRecommendationAiDependencyInjection
{
    public static IServiceCollection AddAlternativeRecommendationAi(
        this IServiceCollection services,
        string modelId,
        string apiKey)
    {
        ArgumentNullException.ThrowIfNull(
            services);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            modelId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            apiKey);

        services.AddOpenAIChatCompletion(
            modelId,
            apiKey);

        services.AddScoped<
            IAlternativeRecommendationGenerator,
            SemanticKernelAlternativeRecommendationGenerator>();

        return services;
    }
}