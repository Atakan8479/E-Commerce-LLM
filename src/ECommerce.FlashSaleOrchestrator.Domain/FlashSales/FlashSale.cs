using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.FlashSales;

public sealed class FlashSale
{
    private readonly HashSet<ProductId> _eligibleProductIds = [];

    public FlashSaleId Id { get; }

    public DateTimeOffset StartsAtUtc { get; }

    public DateTimeOffset EndsAtUtc { get; }

    public IReadOnlyCollection<ProductId> EligibleProductIds =>
        _eligibleProductIds;

    private FlashSale(
        FlashSaleId id,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc)
    {
        Id = id;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
    }

    public static FlashSale Create(
        FlashSaleId id,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
    {
        ArgumentNullException.ThrowIfNull(id);

        var startsAtUtc =
            startsAt.ToUniversalTime();

        var endsAtUtc =
            endsAt.ToUniversalTime();

        if (endsAtUtc <= startsAtUtc)
        {
            throw new ArgumentException(
                "Flash sale end time must be later than start time.",
                nameof(endsAt));
        }

        return new FlashSale(
            id,
            startsAtUtc,
            endsAtUtc);
    }

    public bool IsActiveAt(DateTimeOffset timestamp)
    {
        var timestampUtc =
            timestamp.ToUniversalTime();

        return timestampUtc >= StartsAtUtc &&
               timestampUtc < EndsAtUtc;
    }

    public bool AddEligibleProduct(ProductId productId)
    {
        ArgumentNullException.ThrowIfNull(productId);

        return _eligibleProductIds.Add(productId);
    }

    public bool RemoveEligibleProduct(ProductId productId)
    {
        ArgumentNullException.ThrowIfNull(productId);

        return _eligibleProductIds.Remove(productId);
    }

    public bool IsProductEligible(ProductId productId)
    {
        ArgumentNullException.ThrowIfNull(productId);

        return _eligibleProductIds.Contains(productId);
    }
}