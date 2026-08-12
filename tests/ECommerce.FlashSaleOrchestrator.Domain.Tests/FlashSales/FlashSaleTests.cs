using ECommerce.FlashSaleOrchestrator.Domain.FlashSales;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.Tests.FlashSales;

public sealed class FlashSaleTests
{
    [Fact]
    public void Create_ShouldPreserveIdentifierAndNormalizeScheduleToUtc()
    {
        var flashSaleId =
            FlashSaleId.New();

        var startsAt =
            new DateTimeOffset(
                2026,
                8,
                11,
                18,
                0,
                0,
                TimeSpan.FromHours(3));

        var endsAt =
            startsAt.AddHours(1);

        var flashSale =
            FlashSale.Create(
                flashSaleId,
                startsAt,
                endsAt);

        Assert.Equal(
            flashSaleId,
            flashSale.Id);

        Assert.Equal(
            startsAt.ToUniversalTime(),
            flashSale.StartsAtUtc);

        Assert.Equal(
            endsAt.ToUniversalTime(),
            flashSale.EndsAtUtc);
    }

    [Fact]
    public void Create_ShouldThrowArgumentNullException_WhenIdentifierIsNull()
    {
        var startsAt =
            DateTimeOffset.UtcNow;

        var exception =
            Assert.Throws<ArgumentNullException>(
                () => FlashSale.Create(
                    null!,
                    startsAt,
                    startsAt.AddHours(1)));

        Assert.Equal(
            "id",
            exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldThrowArgumentException_WhenEndTimeIsNotLaterThanStartTime(
        int endOffsetMinutes)
    {
        var startsAt =
            DateTimeOffset.UtcNow;

        var endsAt =
            startsAt.AddMinutes(
                endOffsetMinutes);

        var exception =
            Assert.Throws<ArgumentException>(
                () => FlashSale.Create(
                    FlashSaleId.New(),
                    startsAt,
                    endsAt));

        Assert.Equal(
            "endsAt",
            exception.ParamName);
    }

    [Fact]
    public void IsActiveAt_ShouldReturnTrue_AtStartTime()
    {
        var startsAt =
            DateTimeOffset.UtcNow;

        var flashSale =
            FlashSale.Create(
                FlashSaleId.New(),
                startsAt,
                startsAt.AddHours(1));

        Assert.True(
            flashSale.IsActiveAt(startsAt));
    }

    [Fact]
    public void IsActiveAt_ShouldReturnTrue_DuringFlashSale()
    {
        var startsAt =
            DateTimeOffset.UtcNow;

        var flashSale =
            FlashSale.Create(
                FlashSaleId.New(),
                startsAt,
                startsAt.AddHours(1));

        Assert.True(
            flashSale.IsActiveAt(
                startsAt.AddMinutes(30)));
    }

    [Fact]
    public void IsActiveAt_ShouldReturnFalse_BeforeStartTime()
    {
        var startsAt =
            DateTimeOffset.UtcNow;

        var flashSale =
            FlashSale.Create(
                FlashSaleId.New(),
                startsAt,
                startsAt.AddHours(1));

        Assert.False(
            flashSale.IsActiveAt(
                startsAt.AddTicks(-1)));
    }

    [Fact]
    public void IsActiveAt_ShouldReturnFalse_AtEndTime()
    {
        var startsAt =
            DateTimeOffset.UtcNow;

        var endsAt =
            startsAt.AddHours(1);

        var flashSale =
            FlashSale.Create(
                FlashSaleId.New(),
                startsAt,
                endsAt);

        Assert.False(
            flashSale.IsActiveAt(endsAt));
    }

    [Fact]
    public void AddEligibleProduct_ShouldAddProduct()
    {
        var flashSale =
            CreateFlashSale();

        var productId =
            ProductId.New();

        var added =
            flashSale.AddEligibleProduct(
                productId);

        Assert.True(added);

        Assert.True(
            flashSale.IsProductEligible(
                productId));

        Assert.Contains(
            productId,
            flashSale.EligibleProductIds);
    }

    [Fact]
    public void AddEligibleProduct_ShouldNotCreateDuplicateProduct()
    {
        var flashSale =
            CreateFlashSale();

        var productId =
            ProductId.New();

        var firstResult =
            flashSale.AddEligibleProduct(
                productId);

        var secondResult =
            flashSale.AddEligibleProduct(
                productId);

        Assert.True(firstResult);
        Assert.False(secondResult);

        Assert.Single(
            flashSale.EligibleProductIds);
    }

    [Fact]
    public void AddEligibleProduct_ShouldThrowArgumentNullException_WhenProductIdIsNull()
    {
        var flashSale =
            CreateFlashSale();

        var exception =
            Assert.Throws<ArgumentNullException>(
                () => flashSale.AddEligibleProduct(
                    null!));

        Assert.Equal(
            "productId",
            exception.ParamName);
    }

    [Fact]
    public void RemoveEligibleProduct_ShouldRemoveExistingProduct()
    {
        var flashSale =
            CreateFlashSale();

        var productId =
            ProductId.New();

        flashSale.AddEligibleProduct(
            productId);

        var removed =
            flashSale.RemoveEligibleProduct(
                productId);

        Assert.True(removed);

        Assert.False(
            flashSale.IsProductEligible(
                productId));
    }

    [Fact]
    public void RemoveEligibleProduct_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        var flashSale =
            CreateFlashSale();

        var removed =
            flashSale.RemoveEligibleProduct(
                ProductId.New());

        Assert.False(removed);
    }

    [Fact]
    public void IsProductEligible_ShouldReturnFalse_WhenProductWasNotAdded()
    {
        var flashSale =
            CreateFlashSale();

        Assert.False(
            flashSale.IsProductEligible(
                ProductId.New()));
    }

    private static FlashSale CreateFlashSale()
    {
        var startsAt =
            DateTimeOffset.UtcNow;

        return FlashSale.Create(
            FlashSaleId.New(),
            startsAt,
            startsAt.AddHours(1));
    }
}