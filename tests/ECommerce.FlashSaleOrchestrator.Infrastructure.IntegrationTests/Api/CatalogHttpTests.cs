using System.Net;
using System.Net.Http.Json;
using ECommerce.FlashSaleOrchestrator.Api
    .Contracts.Catalog;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .IntegrationTests.Api;

public sealed class CatalogHttpTests
{
    [Fact]
    public async Task
        GetCatalog_ShouldReturnProductsWithInventory()
    {
        await using var database =
            await ApiTestDatabase.CreateAsync();

        var depletedProductId =
            ProductId.New();

        var availableProductId =
            ProductId.New();

        var productWithoutInventoryId =
            ProductId.New();

        await using (var seedContext =
            database.CreateContext())
        {
            seedContext.Products.AddRange(
                Product.Create(
                    depletedProductId,
                    ProductName.From(
                        "Gaming Mouse"),
                    ProductCategory.From(
                        "gaming-mouse")),
                Product.Create(
                    availableProductId,
                    ProductName.From(
                        "Wireless Gaming Mouse"),
                    ProductCategory.From(
                        "gaming-mouse")),
                Product.Create(
                    productWithoutInventoryId,
                    ProductName.From(
                        "Unavailable Catalog Product"),
                    ProductCategory.From(
                        "gaming-mouse")));

            seedContext.InventoryItems.AddRange(
                InventoryItem.Create(
                    depletedProductId,
                    StockQuantity.From(
                        0)),
                InventoryItem.Create(
                    availableProductId,
                    StockQuantity.From(
                        15)));

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

        using var response =
            await client.GetAsync(
                "/api/catalog");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var payload =
            await response.Content
                .ReadFromJsonAsync<CatalogResponse>();

        Assert.NotNull(
            payload);

        Assert.Equal(
            2,
            payload.Items.Count);

        Assert.Equal(
            depletedProductId.Value,
            payload.Items[0].ProductId);

        Assert.Equal(
            "Gaming Mouse",
            payload.Items[0].Name);

        Assert.Equal(
            "gaming-mouse",
            payload.Items[0].Category);

        Assert.Equal(
            0,
            payload.Items[0].AvailableQuantity);

        Assert.True(
            payload.Items[0].IsDepleted);

        Assert.Equal(
            availableProductId.Value,
            payload.Items[1].ProductId);

        Assert.Equal(
            15,
            payload.Items[1].AvailableQuantity);

        Assert.False(
            payload.Items[1].IsDepleted);

        Assert.DoesNotContain(
            payload.Items,
            item =>
                item.ProductId ==
                productWithoutInventoryId.Value);
    }
}