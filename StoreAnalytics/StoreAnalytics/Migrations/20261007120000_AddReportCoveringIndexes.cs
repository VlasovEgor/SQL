using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAnalytics.Migrations
{
    /// <inheritdoc />
    public partial class AddReportCoveringIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                    name: "IX_OrderItems_OrderId_Report",
                    table: "OrderItems",
                    column: "OrderId")
                .Annotation("Npgsql:IndexInclude", new[] { "Quantity", "Price" });

            migrationBuilder.CreateIndex(
                    name: "IX_Orders_OrderDate_Report",
                    table: "Orders",
                    column: "OrderDate")
                .Annotation("Npgsql:IndexInclude", new[] { "OrderId", "CustomerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId_Report",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderDate_Report",
                table: "Orders");
        }
    }
}
