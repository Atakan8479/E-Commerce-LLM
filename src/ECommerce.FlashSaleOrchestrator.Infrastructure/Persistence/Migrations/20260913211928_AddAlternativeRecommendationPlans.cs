using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAlternativeRecommendationPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlternativeRecommendationPlans",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlternativeRecommendationPlans", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlternativeRecommendationPlans_CorrelationId",
                table: "AlternativeRecommendationPlans",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AlternativeRecommendationPlans_OriginalProductId",
                table: "AlternativeRecommendationPlans",
                column: "OriginalProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlternativeRecommendationPlans");
        }
    }
}
