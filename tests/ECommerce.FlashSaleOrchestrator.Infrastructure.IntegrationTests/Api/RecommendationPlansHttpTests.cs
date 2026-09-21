using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.FlashSaleOrchestrator.Api
    .Contracts.RecommendationPlans;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence.AlternativeRecommendations;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .IntegrationTests.Api;

public sealed class RecommendationPlansHttpTests
{
    [Fact]
    public async Task
        GetByCorrelationId_ShouldReturnEmptyPlansAndPreserveCorrelationId_WhenPlanIsNotReady()
    {
        await using var database =
            await ApiTestDatabase.CreateAsync();

        using var factory =
            new ApiWebApplicationFactory(
                database.ConnectionString);

        using var client =
            CreateClient(
                factory);

        var correlationId =
            $"http-{Guid.NewGuid():N}";

        using var request =
            CreateRequest(
                correlationId);

        using var response =
            await client.SendAsync(
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.True(
            response.Headers.TryGetValues(
                "X-Correlation-ID",
                out var correlationHeaderValues));

        Assert.Equal(
            correlationId,
            Assert.Single(
                correlationHeaderValues!));

        var payload =
            await response.Content
                .ReadFromJsonAsync<
                    RecommendationPlansResponse>();

        Assert.NotNull(
            payload);

        Assert.Equal(
            correlationId,
            payload.CorrelationId);

        Assert.Empty(
            payload.Plans);
    }

    [Fact]
    public async Task
        GetByCorrelationId_ShouldReturnPersistedRecommendationPlan()
    {
        await using var database =
            await ApiTestDatabase.CreateAsync();

        var eventId =
            Guid.NewGuid();

        var originalProductId =
            Guid.NewGuid();

        var recommendedProductId =
            Guid.NewGuid();

        var correlationId =
            $"http-plan-{Guid.NewGuid():N}";

        var createdAtUtc =
            new DateTime(
                2026,
                9,
                20,
                18,
                30,
                0,
                DateTimeKind.Utc);

        var recommendationResult =
            new AlternativeRecommendationResult(
                new[]
                {
                    new AlternativeRecommendation(
                        recommendedProductId,
                        "Suitable in-stock alternative.")
                });

        var payloadJson =
            JsonSerializer.Serialize(
                recommendationResult,
                new JsonSerializerOptions(
                    JsonSerializerDefaults.Web));

        await using (var seedContext =
            database.CreateContext())
        {
            seedContext
                .AlternativeRecommendationPlans
                .Add(
                    new AlternativeRecommendationPlanRecord(
                        eventId,
                        originalProductId,
                        payloadJson,
                        AlternativeRecommendationSource.Llm,
                        correlationId,
                        createdAtUtc));

            await seedContext
                .SaveChangesAsync();
        }

        using var factory =
            new ApiWebApplicationFactory(
                database.ConnectionString);

        using var client =
            CreateClient(
                factory);

        using var request =
            CreateRequest(
                correlationId);

        using var response =
            await client.SendAsync(
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var payload =
            await response.Content
                .ReadFromJsonAsync<
                    RecommendationPlansResponse>();

        Assert.NotNull(
            payload);

        Assert.Equal(
            correlationId,
            payload.CorrelationId);

        var plan =
            Assert.Single(
                payload.Plans);

        Assert.Equal(
            eventId,
            plan.EventId);

        Assert.Equal(
            originalProductId,
            plan.OriginalProductId);

        Assert.Equal(
            "Llm",
            plan.Source);

        Assert.Equal(
            createdAtUtc,
            plan.CreatedAtUtc);

        var recommendation =
            Assert.Single(
                plan.Recommendations);

        Assert.Equal(
            recommendedProductId,
            recommendation.ProductId);

        Assert.Equal(
            "Suitable in-stock alternative.",
            recommendation.Reason);
    }

    private static HttpClient CreateClient(
        ApiWebApplicationFactory factory)
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress =
                    new Uri(
                        "https://localhost")
            });
    }

    private static HttpRequestMessage CreateRequest(
        string correlationId)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/recommendation-plans" +
                $"?correlationId=" +
                Uri.EscapeDataString(
                    correlationId));

        request.Headers.TryAddWithoutValidation(
            "X-Correlation-ID",
            correlationId);

        return request;
    }
}