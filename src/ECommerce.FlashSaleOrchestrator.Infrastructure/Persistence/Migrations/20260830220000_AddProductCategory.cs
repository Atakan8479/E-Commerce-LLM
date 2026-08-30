using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Migrations;

[DbContext(typeof(FlashSaleOrchestratorDbContext))]
[Migration("20260830220000_AddProductCategory")]
public sealed class AddProductCategory : Migration
{
    protected override void Up(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Category",
            table: "Products",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValueSql: "N'uncategorized'");
    }

    protected override void Down(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Category",
            table: "Products");
    }
}