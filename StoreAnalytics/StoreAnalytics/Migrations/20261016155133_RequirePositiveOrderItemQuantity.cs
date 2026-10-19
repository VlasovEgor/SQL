using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAnalytics.Migrations
{
    /// <inheritdoc />
    public partial class RequirePositiveOrderItemQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve existing data: require an explicit correction of invalid legacy rows.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "OrderItems" WHERE "Quantity" <= 0) THEN
                        RAISE EXCEPTION 'OrderItems contains non-positive quantities. Correct these rows before applying RequirePositiveOrderItemQuantity.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_Items_Quantity_Min",
                table: "OrderItems");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_Items_Quantity_Min",
                table: "OrderItems",
                sql: "\"Quantity\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_Items_Quantity_Min",
                table: "OrderItems");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_Items_Quantity_Min",
                table: "OrderItems",
                sql: "\"Quantity\" >= 0");
        }
    }
}
