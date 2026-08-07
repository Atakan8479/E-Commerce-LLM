using ECommerce.FlashSaleOrchestrator.Domain.Abstractions;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory.Events;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory.Exceptions;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.Inventory;

public sealed class InventoryItem
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public ProductId ProductId { get; }

    public StockQuantity AvailableQuantity { get; private set; }

    public bool IsDepleted => AvailableQuantity.Value == 0;

    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    private InventoryItem(
        ProductId productId,
        StockQuantity availableQuantity)
    {
        ProductId = productId;
        AvailableQuantity = availableQuantity;
    }

    public static InventoryItem Create(
        ProductId productId,
        StockQuantity availableQuantity)
    {
        ArgumentNullException.ThrowIfNull(productId);
        ArgumentNullException.ThrowIfNull(availableQuantity);

        return new InventoryItem(
            productId,
            availableQuantity);
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity,
                "Stock increase quantity must be greater than zero.");
        }

        var newQuantity = checked(
            AvailableQuantity.Value + quantity);

        AvailableQuantity =
            StockQuantity.From(newQuantity);
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity,
                "Stock decrease quantity must be greater than zero.");
        }

        if (quantity > AvailableQuantity.Value)
        {
            throw new InsufficientStockException(
                ProductId,
                AvailableQuantity.Value,
                quantity);
        }

        var previousQuantity =
            AvailableQuantity.Value;

        var newQuantity =
            previousQuantity - quantity;

        AvailableQuantity =
            StockQuantity.From(newQuantity);

        if (previousQuantity > 0 &&
            newQuantity == 0)
        {
            _domainEvents.Add(
                new StockDepletedDomainEvent(ProductId));
        }
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}