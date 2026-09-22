using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.FlashSaleOrchestrator.Api.Contracts.Carts;
using ECommerce.FlashSaleOrchestrator.Api.Contracts.Inventory;
using ECommerce.FlashSaleOrchestrator.Api.Contracts.Products;
using ECommerce.FlashSaleOrchestrator.Domain.Carts;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .IntegrationTests.Api;

public sealed class ReadEndpointsHttpTests
{
    [Fact]
    public async Task
        GetProduct_ShouldReturnPersistedProduct()
    {
        await using var database =
            await ApiTestDatabase.CreateAsync();

        var productId =
            Guid.NewGuid();

        await using (var seedContext =
            database.CreateContext())
        {
            seedContext.Products.Add(
                Product.Create(
                    ProductId.From(productId),
                    ProductName.From(
                        "Wireless Gaming Mouse"),
                    ProductCategory.From(
                        "gaming-mouse")));

            await seedContext
                .SaveChangesAsync();
        }

        using var factory =
            new ApiWebApplicationFactory(
                database.ConnectionString);

        using var client =
            CreateClient(
                factory);

        using var response =
            await client.GetAsync(
                $"/api/products/{productId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var payload =
            await response.Content
                .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(
            payload);

        Assert.Equal(
            productId,
            payload.ProductId);

        Assert.Equal(
            "Wireless Gaming Mouse",
            payload.Name);

        Assert.Equal(
            "gaming-mouse",
            payload.Category);
    }

    [Fact]
    public async Task
        GetInventory_ShouldReturnPersistedInventory()
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
                        "Mechanical Keyboard"),
                    ProductCategory.From(
                        "keyboard")));

            seedContext.InventoryItems.Add(
                InventoryItem.Create(
                    domainProductId,
                    StockQuantity.From(
                        7)));

            await seedContext
                .SaveChangesAsync();
        }

        using var factory =
            new ApiWebApplicationFactory(
                database.ConnectionString);

        using var client =
            CreateClient(
                factory);

        using var response =
            await client.GetAsync(
                $"/api/inventory/{productId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var payload =
            await response.Content
                .ReadFromJsonAsync<InventoryResponse>();

        Assert.NotNull(
            payload);

        Assert.Equal(
            productId,
            payload.ProductId);

        Assert.Equal(
            7,
            payload.AvailableQuantity);

        Assert.False(
            payload.IsDepleted);
    }

    [Fact]
    public async Task
        GetCart_ShouldReturnPersistedCartWithItems()
    {
        await using var database =
            await ApiTestDatabase.CreateAsync();

        var productId =
            Guid.NewGuid();

        var cartId =
            Guid.NewGuid();

        var domainProductId =
            ProductId.From(
                productId);

        var cart =
            Cart.Create(
                CartId.From(
                    cartId));

        cart.AddItem(
            domainProductId,
            2);

        await using (var seedContext =
            database.CreateContext())
        {
            seedContext.Products.Add(
                Product.Create(
                    domainProductId,
                    ProductName.From(
                        "USB-C Hub"),
                    ProductCategory.From(
                        "accessories")));

            seedContext.Carts.Add(
                cart);

            await seedContext
                .SaveChangesAsync();
        }

        using var factory =
            new ApiWebApplicationFactory(
                database.ConnectionString);

        using var client =
            CreateClient(
                factory);

        using var response =
            await client.GetAsync(
                $"/api/carts/{cartId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var payload =
            await response.Content
                .ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(
            payload);

        Assert.Equal(
            cartId,
            payload.CartId);

        var item =
            Assert.Single(
                payload.Items);

        Assert.Equal(
            productId,
            item.ProductId);

        Assert.Equal(
            2,
            item.Quantity);
    }

    [Fact]
    public async Task
        GetProduct_ShouldReturnStableProblemDetails_WhenProductDoesNotExist()
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
            $"missing-product-{Guid.NewGuid():N}";

        var requestPath =
            $"/api/products/{productId}";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                requestPath);

        request.Headers.TryAddWithoutValidation(
            "X-Correlation-ID",
            correlationId);

        using var response =
            await client.SendAsync(
                request);

        await AssertNotFoundProblemDetailsAsync(
            response,
            "Product not found.",
            "product-not-found",
            $"Product '{productId}' was not found.",
            requestPath,
            correlationId);
    }

    [Fact]
    public async Task
        GetInventory_ShouldReturnStableProblemDetails_WhenInventoryDoesNotExist()
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
            $"missing-inventory-{Guid.NewGuid():N}";

        var requestPath =
            $"/api/inventory/{productId}";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                requestPath);

        request.Headers.TryAddWithoutValidation(
            "X-Correlation-ID",
            correlationId);

        using var response =
            await client.SendAsync(
                request);

        await AssertNotFoundProblemDetailsAsync(
            response,
            "Inventory item not found.",
            "inventory-item-not-found",
            $"Inventory item for product " +
            $"'{productId}' was not found.",
            requestPath,
            correlationId);
    }

    [Fact]
    public async Task
        GetCart_ShouldReturnStableProblemDetails_WhenCartDoesNotExist()
    {
        await using var database =
            await ApiTestDatabase.CreateAsync();

        var cartId =
            Guid.NewGuid();

        using var factory =
            new ApiWebApplicationFactory(
                database.ConnectionString);

        using var client =
            CreateClient(
                factory);

        var correlationId =
            $"missing-cart-{Guid.NewGuid():N}";

        var requestPath =
            $"/api/carts/{cartId}";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                requestPath);

        request.Headers.TryAddWithoutValidation(
            "X-Correlation-ID",
            correlationId);

        using var response =
            await client.SendAsync(
                request);

        await AssertNotFoundProblemDetailsAsync(
            response,
            "Cart not found.",
            "cart-not-found",
            $"Cart '{cartId}' was not found.",
            requestPath,
            correlationId);
    }

    private static async Task
        AssertNotFoundProblemDetailsAsync(
            HttpResponseMessage response,
            string expectedTitle,
            string expectedErrorCode,
            string expectedDetail,
            string expectedInstance,
            string expectedCorrelationId)
    {
        Assert.Equal(
            HttpStatusCode.NotFound,
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
            (int)HttpStatusCode.NotFound,
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
}