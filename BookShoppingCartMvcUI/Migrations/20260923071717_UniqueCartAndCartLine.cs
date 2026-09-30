using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookShoppingCartMvcUI.Migrations
{
    /// <inheritdoc />
    public partial class UniqueCartAndCartLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Duplicates made by parallel "add to cart" requests would stop the unique indexes from being created.
            // Merge them without losing anything the customer added:
            // 1. move every line into the customer's oldest cart,
            migrationBuilder.Sql("""
                UPDATE cd SET [ShoppingCartId] = keep.[Id]
                FROM [CartDetail] cd
                JOIN [ShoppingCart] c ON c.[Id] = cd.[ShoppingCartId]
                JOIN (SELECT [UserId], MIN([Id]) AS [Id] FROM [ShoppingCart] GROUP BY [UserId]) keep ON keep.[UserId] = c.[UserId]
                WHERE cd.[ShoppingCartId] <> keep.[Id];
                """);
            // 2. delete the other, now empty, carts,
            migrationBuilder.Sql("""
                DELETE c FROM [ShoppingCart] c
                WHERE c.[Id] <> (SELECT MIN(k.[Id]) FROM [ShoppingCart] k WHERE k.[UserId] = c.[UserId]);
                """);
            // 3. give the oldest line of each book the total quantity, then delete the other lines.
            //    The cart page flags a total above the stock, and checkout refuses it.
            migrationBuilder.Sql("""
                UPDATE cd SET [Quantity] = t.[Total]
                FROM [CartDetail] cd
                JOIN (SELECT MIN([Id]) AS [Id], SUM([Quantity]) AS [Total]
                      FROM [CartDetail] GROUP BY [ShoppingCartId], [BookId] HAVING COUNT(*) > 1) t ON t.[Id] = cd.[Id];
                """);
            migrationBuilder.Sql("""
                DELETE cd FROM [CartDetail] cd
                WHERE cd.[Id] <> (SELECT MIN(k.[Id]) FROM [CartDetail] k
                                  WHERE k.[ShoppingCartId] = cd.[ShoppingCartId] AND k.[BookId] = cd.[BookId]);
                """);

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCart_UserId",
                table: "ShoppingCart");

            migrationBuilder.DropIndex(
                name: "IX_CartDetail_ShoppingCartId",
                table: "CartDetail");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCart_UserId",
                table: "ShoppingCart",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartDetail_ShoppingCartId_BookId",
                table: "CartDetail",
                columns: new[] { "ShoppingCartId", "BookId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoppingCart_UserId",
                table: "ShoppingCart");

            migrationBuilder.DropIndex(
                name: "IX_CartDetail_ShoppingCartId_BookId",
                table: "CartDetail");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCart_UserId",
                table: "ShoppingCart",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CartDetail_ShoppingCartId",
                table: "CartDetail",
                column: "ShoppingCartId");
        }
    }
}
