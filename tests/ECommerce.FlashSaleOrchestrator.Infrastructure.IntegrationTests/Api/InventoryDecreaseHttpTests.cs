using ECommerce.FlashSaleOrchestrator.Api.Contracts.Inventory;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.Inventory.DecreaseStock;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory.Events;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .IntegrationTests.Api;

public sealed class InventoryDecreaseHttpTests
{
    [Fact]
    public async Task
        DecreaseStock_ShouldPersistCorrelatedOutboxMessage_WhenStockBecomesDepleted()
    {
        await using var database =
            await ApiTestDatabase.CreateAsync();

        var productId =
            Guid.NewGuid();

        var domainProductId =
            ProductId.From(
                productId);

        await using (var seedContext =
            database.CreateContext())
        {
            seedContext.Products.Add(
                Product.Create(
                    domainProductId,
                    ProductName.From(
                        "Batch 7B Product")));

            seedContext.InventoryItems.Add(
                InventoryItem.Create(
                    domainProductId,
                    StockQuantity.From(
                        1)));

            await seedContext
                .SaveChangesAsync();
        }

        using var factory =
            new ApiWebApplicationFactory(
                database.ConnectionString);

        using var client =
            CreateClient(
                factory);

        var correlationId =
            $"batch7b-{Guid.NewGuid():N}";

        using var request =
            CreateDecreaseRequest(
                productId,
                1,
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
                    DecreaseStockResponse>();

        Assert.NotNull(
            payload);

        Assert.Equal(
            productId,
            payload.ProductId);

        Assert.Equal(
            0,
            payload.RemainingQuantity);

        Assert.True(
            payload.IsDepleted);

        Assert.True(
            payload.RecommendationRequested);

        Assert.Equal(
            correlationId,
            payload.CorrelationId);

        await using var verificationContext =
            database.CreateContext();

        var persistedInventory =
            await verificationContext
                .InventoryItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            productId,
            persistedInventory.ProductId.Value);

        Assert.Equal(
            0,
            persistedInventory.AvailableQuantity.Value);

        Assert.True(
            persistedInventory.IsDepleted);

        var outboxMessages =
            await verificationContext
                .OutboxMessages
                .AsNoTracking()
                .ToArrayAsync();

        var outboxMessage =
            Assert.Single(
                outboxMessages);

        Assert.Equal(
            correlationId,
            outboxMessage.CorrelationId);

        Assert.Equal(
            typeof(StockDepletedDomainEvent).FullName,
            outboxMessage.Type);

        Assert.Null(
            outboxMessage.ProcessedAtUtc);
    }

    [Fact]
    public async Task
        DecreaseStock_ShouldReturnConflict_WhenInventoryConcurrencyOccurs()
    {
        await using var database =
            await ApiTestDatabase.CreateAsync();

        var productId =
            Guid.NewGuid();

        using var factory =
            new ApiWebApplicationFactory(
                database.ConnectionString);

        using var concurrencyFactory =
            factory.WithWebHostBuilder(
                builder =>
                {
                    builder.ConfigureServices(
                        services =>
                        {
                            services.RemoveAll<
                                ICommandHandler<
                                    DecreaseStockCommand,
                                    DecreaseStockResult>>();

                            services.AddScoped<
                                ICommandHandler<
                                    DecreaseStockCommand,
                                    DecreaseStockResult>,
                                ConcurrencyThrowingDecreaseStockCommandHandler>();
                        });
                });

        using var client =
            CreateClient(
                concurrencyFactory);

        var correlationId =
            $"concurrency-{Guid.NewGuid():N}";

        using var request =
            CreateDecreaseRequest(
                productId,
                1,
                correlationId);

        using var response =
            await client.SendAsync(
                request);

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Conflict,
            "Inventory update conflict.",
            "inventory-concurrency-conflict",
            "Inventory changed while this request was being processed. " +
            "Refresh the current inventory state and retry if appropriate.",
            $"/api/inventory/{productId}/decrease",
            correlationId);
    }

    [Fact]
    public async Task
        DecreaseStock_ShouldReturnStableNotFoundProblemDetails_WhenInventoryDoesNotExist()
    {
        await using var database =
            await ApiTestDatabase.CreateAsync();

        var productId =
            Guid.NewGuid();

        using var factory =
            new ApiWebApplicationFactory(
                database.ConnectionString);

        using var client =
            CreateClient(
                factory);

        var correlationId =
            $"inventory-not-found-{Guid.NewGuid():N}";

        using var request =
            CreateDecreaseRequest(
                productId,
                1,
                correlationId);

        using var response =
            await client.SendAsync(
                request);

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Inventory item not found.",
            "inventory-item-not-found",
            "The requested inventory item was not found.",
            $"/api/inventory/{productId}/decrease",
            correlationId);
    }

    [Fact]
    public async Task
        DecreaseStock_ShouldReturnStableConflictProblemDetails_WhenStockIsInsufficient()
    {
        await using var database =
            await ApiTestDatabase.CreateAsync();

        var productId =
            Guid.NewGuid();

        var domainProductId =
            ProductId.From(
                productId);

        await using (var seedContext =
            database.CreateContext())
        {
            seedContext.Products.Add(
                Product.Create(
                    domainProductId,
                    ProductName.From(
                        "Low Stock Product")));

            seedContext.InventoryItems.Add(
                InventoryItem.Create(
                    domainProductId,
                    StockQuantity.From(
                        1)));

            await seedContext
                .SaveChangesAsync();
        }

        using var factory =
            new ApiWebApplicationFactory(
                database.ConnectionString);

        using var client =
            CreateClient(
                factory);

        var correlationId =
            $"insufficient-stock-{Guid.NewGuid():N}";

        using var request =
            CreateDecreaseRequest(
                productId,
                2,
                correlationId);

        using var response =
            await client.SendAsync(
                request);

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Conflict,
            "Insufficient stock.",
            "insufficient-stock",
            "The requested quantity is not available.",
            $"/api/inventory/{productId}/decrease",
            correlationId);
    }

    [Fact]
    public async Task
        DecreaseStock_ShouldReturnStableBadRequestProblemDetails_WhenProductIdIsEmpty()
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
            $"invalid-request-{Guid.NewGuid():N}";

        using var request =
            CreateDecreaseRequest(
                Guid.Empty,
                1,
                correlationId);

        using var response =
            await client.SendAsync(
                request);

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "Invalid request.",
            "invalid-request",
            "The request contains an invalid value.",
            $"/api/inventory/{Guid.Empty}/decrease",
            correlationId);
    }

    private static HttpRequestMessage
        CreateDecreaseRequest(
            Guid productId,
            int quantity,
            string correlationId)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/inventory/{productId}/decrease");

        request.Headers.TryAddWithoutValidation(
            "X-Correlation-ID",
            correlationId);

        request.Content =
            JsonContent.Create(
                new DecreaseStockRequest
                {
                    Quantity =
                        quantity
                });

        return request;
    }

    private static async Task
        AssertProblemDetailsAsync(
            HttpResponseMessage response,
            HttpStatusCode expectedStatusCode,
            string expectedTitle,
            string expectedErrorCode,
            string expectedDetail,
            string expectedInstance,
            string expectedCorrelationId)
    {
        Assert.Equal(
            expectedStatusCode,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers
                .ContentType?
                .MediaType);

        Assert.True(
            response.Headers.TryGetValues(
                "X-Correlation-ID",
                out var correlationHeaderValues));

        Assert.Equal(
            expectedCorrelationId,
            Assert.Single(
                correlationHeaderValues!));

        using var payload =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        var root =
            payload.RootElement;

        Assert.Equal(
            (int)expectedStatusCode,
            root.GetProperty(
                "status")
                .GetInt32());

        Assert.Equal(
            expectedTitle,
            root.GetProperty(
                "title")
                .GetString());

        Assert.Equal(
            $"urn:flashsale:error:{expectedErrorCode}",
            root.GetProperty(
                "type")
                .GetString());

        Assert.Equal(
            expectedDetail,
            root.GetProperty(
                "detail")
                .GetString());

        Assert.Equal(
            expectedInstance,
            root.GetProperty(
                "instance")
                .GetString());

        Assert.Equal(
            expectedErrorCode,
            root.GetProperty(
                "errorCode")
                .GetString());

        Assert.Equal(
            expectedCorrelationId,
            root.GetProperty(
                "correlationId")
                .GetString());
    }

    private static HttpClient CreateClient(
        WebApplicationFactory<global::Program> factory)
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

    private sealed class
        ConcurrencyThrowingDecreaseStockCommandHandler
        : ICommandHandler<
            DecreaseStockCommand,
            DecreaseStockResult>
    {
        public Task<DecreaseStockResult> HandleAsync(
            DecreaseStockCommand command,
            CancellationToken cancellationToken = default)
        {
            throw new InventoryConcurrencyException(
                command.ProductId);
        }
    }
}