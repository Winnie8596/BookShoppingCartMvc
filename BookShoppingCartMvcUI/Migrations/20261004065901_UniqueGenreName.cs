using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookShoppingCartMvcUI.Migrations
{
    /// <inheritdoc />
    public partial class UniqueGenreName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // an older db could already have the same name twice (the name check used to be in the app only).
            // keep the first one as is and add the id to the others, so the index can be created
            migrationBuilder.Sql(@"
UPDATE g
SET GenreName = LEFT(g.GenreName, 40 - LEN(' (' + CAST(g.Id AS nvarchar(11)) + ')')) + ' (' + CAST(g.Id AS nvarchar(11)) + ')'
FROM [Genre] g
WHERE EXISTS (SELECT 1 FROM [Genre] older WHERE older.GenreName = g.GenreName AND older.Id < g.Id)");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_GenreName",
                table: "Genre",
                column: "GenreName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Genre_GenreName",
                table: "Genre");
        }
    }
}
