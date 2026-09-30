using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookShoppingCartMvcUI.Migrations
{
    /// <inheritdoc />
    public partial class AddStockQuantityCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // rows made negative by the old read-then-write checkout would stop the constraint from being added
            migrationBuilder.Sql("UPDATE [Stock] SET [Quantity] = 0 WHERE [Quantity] < 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Stock_Quantity",
                table: "Stock",
                sql: "[Quantity] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Stock_Quantity",
                table: "Stock");
        }
    }
}
