using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Application.Inventory.DecreaseStock;

public sealed class DecreaseStockCommandHandler
    : ICommandHandler<DecreaseStockCommand, DecreaseStockResult>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DecreaseStockCommandHandler(
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository =
            inventoryRepository
            ?? throw new ArgumentNullException(
                nameof(inventoryRepository));

        _unitOfWork =
            unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
    }

    public async Task<DecreaseStockResult> HandleAsync(
        DecreaseStockCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command.Quantity),
                command.Quantity,
                "Stock decrease quantity must be greater than zero.");
        }

        var productId =
            ProductId.From(command.ProductId);

        var inventoryItem =
            await _inventoryRepository.GetByProductIdAsync(
                productId,
                cancellationToken);

        if (inventoryItem is null)
        {
            throw new InventoryItemNotFoundException(
                command.ProductId);
        }

        inventoryItem.DecreaseStock(
            command.Quantity);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new DecreaseStockResult(
            inventoryItem.ProductId.Value,
            inventoryItem.AvailableQuantity.Value,
            inventoryItem.IsDepleted);
    }
}