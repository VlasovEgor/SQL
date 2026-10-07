using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAnalytics.Migrations
{
    /// <inheritdoc />
    public partial class RenameOrderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrdersId",
                table: "Orders",
                newName: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "Orders",
                newName: "OrdersId");
        }
    }
}
