using System.Net;
using System.Net.Http.Json;
using ECommerce.FlashSaleOrchestrator.Api
    .Contracts.RecommendationPlans;
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
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    BaseAddress =
                        new Uri(
                            "https://localhost")
                });

        var correlationId =
            $"http-{Guid.NewGuid():N}";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/recommendation-plans" +
                $"?correlationId=" +
                Uri.EscapeDataString(
                    correlationId));

        request.Headers.TryAddWithoutValidation(
            "X-Correlation-ID",
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
}