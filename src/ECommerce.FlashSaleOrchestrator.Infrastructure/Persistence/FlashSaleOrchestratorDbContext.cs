using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Domain.Carts;
using ECommerce.FlashSaleOrchestrator.Domain.FlashSales;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence;

public sealed class FlashSaleOrchestratorDbContext
    : DbContext,
      IUnitOfWork
{
    public FlashSaleOrchestratorDbContext(
        DbContextOptions<FlashSaleOrchestratorDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products =>
        Set<Product>();

    public DbSet<InventoryItem> InventoryItems =>
        Set<InventoryItem>();

    public DbSet<Cart> Carts =>
        Set<Cart>();

    public DbSet<FlashSale> FlashSales =>
        Set<FlashSale>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FlashSaleOrchestratorDbContext)
                .Assembly);
    }

    async Task IUnitOfWork.SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await SaveChangesAsync(
            cancellationToken);
    }
}