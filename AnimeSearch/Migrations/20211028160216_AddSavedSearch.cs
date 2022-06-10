using Microsoft.EntityFrameworkCore.Migrations;

namespace AnimeSearch.Migrations
{
    public partial class AddSavedSearch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedSearch",
                columns: table => new
                {
                    SavedSearch = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    User_id = table.Column<int>(type: "int", nullable: false),
                    Resultats = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSearch", x => new { x.SavedSearch, x.User_id });
                    table.ForeignKey(
                        name: "FK_SavedSearchs_Users",
                        column: x => x.User_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearch_User_id",
                table: "SavedSearch",
                column: "User_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedSearch");
        }
    }
}
