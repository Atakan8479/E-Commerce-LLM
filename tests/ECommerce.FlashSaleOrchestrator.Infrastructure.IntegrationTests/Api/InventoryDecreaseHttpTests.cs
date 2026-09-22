using ECommerce.FlashSaleOrchestrator.Api.Contracts.Inventory;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.Inventory.DecreaseStock;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory.Events;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Api
{
    public class InventoryDecreaseHttpTests
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
                factory.CreateClient(
                    new WebApplicationFactoryClientOptions
                    {
                        AllowAutoRedirect = false,
                        BaseAddress =
                            new Uri(
                                "https://localhost")
                    });

            var correlationId =
                $"batch7b-{Guid.NewGuid():N}";

            using var request =
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
                        Quantity = 1
                    });

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
                concurrencyFactory.CreateClient(
                    new WebApplicationFactoryClientOptions
                    {
                        AllowAutoRedirect = false,
                        BaseAddress =
                            new Uri(
                                "https://localhost")
                    });

            var correlationId =
                $"concurrency-{Guid.NewGuid():N}";

            using var request =
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
                        Quantity = 1
                    });

            using var response =
                await client.SendAsync(
                    request);

            Assert.Equal(
                HttpStatusCode.Conflict,
                response.StatusCode);

            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);

            Assert.True(
                response.Headers.TryGetValues(
                    "X-Correlation-ID",
                    out var correlationHeaderValues));

            Assert.Equal(
                correlationId,
                Assert.Single(
                    correlationHeaderValues!));

            using var payload =
                JsonDocument.Parse(
                    await response.Content
                        .ReadAsStringAsync());

            var root =
                payload.RootElement;

            Assert.Equal(
                StatusCodes.Status409Conflict,
                root.GetProperty(
                    "status")
                    .GetInt32());

            Assert.Equal(
                "Inventory update conflict.",
                root.GetProperty(
                    "title")
                    .GetString());

            Assert.Equal(
                "urn:flashsale:error:inventory-concurrency-conflict",
                root.GetProperty(
                    "type")
                    .GetString());

            Assert.Equal(
                "Inventory changed while this request was being processed. " +
                "Refresh the current inventory state and retry if appropriate.",
                root.GetProperty(
                    "detail")
                    .GetString());

            Assert.Equal(
                $"/api/inventory/{productId}/decrease",
                root.GetProperty(
                    "instance")
                    .GetString());

            Assert.Equal(
                "inventory-concurrency-conflict",
                root.GetProperty(
                    "errorCode")
                    .GetString());

            Assert.Equal(
                correlationId,
                root.GetProperty(
                    "correlationId")
                    .GetString());
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
}
