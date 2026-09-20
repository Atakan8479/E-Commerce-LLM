using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .IntegrationTests.Api;

public sealed class ApiHealthChecksTests
{
    private const string UnavailableSqlConnectionString =
        "Server=127.0.0.1,65534;" +
        "Database=Unavailable;" +
        "User Id=sa;" +
        "Password=integration-test-password;" +
        "Encrypt=False;" +
        "TrustServerCertificate=True;" +
        "Connect Timeout=1;";

    [Fact]
    public async Task
        Live_ShouldReturnOk_WithoutCheckingSqlServer()
    {
        using var factory =
            new ApiWebApplicationFactory(
                UnavailableSqlConnectionString);

        using var client =
            CreateClient(
                factory);

        using var response =
            await client.GetAsync(
                "/health/live");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task
        Ready_ShouldReturnOk_WhenSqlServerIsAvailable()
    {
        await using var database =
            await ApiTestDatabase.CreateAsync();

        using var factory =
            new ApiWebApplicationFactory(
                database.ConnectionString);

        using var client =
            CreateClient(
                factory);

        using var response =
            await client.GetAsync(
                "/health/ready");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task
        Ready_ShouldReturnServiceUnavailable_WhenSqlServerIsUnavailable()
    {
        using var factory =
            new ApiWebApplicationFactory(
                UnavailableSqlConnectionString);

        using var client =
            CreateClient(
                factory);

        using var response =
            await client.GetAsync(
                "/health/ready");

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    private static HttpClient CreateClient(
        ApiWebApplicationFactory factory)
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect =
                    false,

                BaseAddress =
                    new Uri(
                        "https://localhost")
            });
    }
}