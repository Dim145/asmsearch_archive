using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimeSearch.Data.Migrations
{
    public partial class AddEpisodesUrls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EpisodesUrls",
                columns: table => new
                {
                    id_api = table.Column<int>(type: "int", nullable: false),
                    search_id = table.Column<int>(type: "int", nullable: false),
                    season = table.Column<int>(type: "int", nullable: false),
                    episode = table.Column<int>(type: "int", nullable: false),
                    url = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    is_valid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodesUrls", x => new { x.id_api, x.search_id, x.season, x.episode, x.url });
                    table.ForeignKey(
                        name: "FK_EpisodesUrls_Apis_id_api",
                        column: x => x.id_api,
                        principalTable: "Apis",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EpisodesUrls");
        }
    }
}
