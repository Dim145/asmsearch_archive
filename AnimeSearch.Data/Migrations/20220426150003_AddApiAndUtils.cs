using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimeSearch.Data.Migrations
{
    public partial class AddApiAndUtils : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiFilterTypes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    label = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiFilterTypes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Apis",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    siteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    apiUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    searchUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    singleSearchUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    globalSearchUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    moviesSearchUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tvSearchUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    animeSearchUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    discoverUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    moviesIdUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tvIdUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    animeIdUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tokenName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pageName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tableFields = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pathToResults = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    genresMoviesUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    genresTvUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    genresPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    otherNamesUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pathToOnResults = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pathInOnResObject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    imageBasePath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apis", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ApiSortTypes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    label = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiSortTypes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ApiObjectFilter",
                columns: table => new
                {
                    idObject = table.Column<int>(type: "int", nullable: false),
                    idApiFilter = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiObjectId = table.Column<int>(type: "int", nullable: true),
                    ApiFilterId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiObjectFilter", x => new { x.idObject, x.idApiFilter });
                    table.ForeignKey(
                        name: "FK_ApiObjectFilter_ApiFilterTypes_ApiFilterId",
                        column: x => x.ApiFilterId,
                        principalTable: "ApiFilterTypes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ApiObjectFilter_Apis_ApiObjectId",
                        column: x => x.ApiObjectId,
                        principalTable: "Apis",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ApiObjectSort",
                columns: table => new
                {
                    idObject = table.Column<int>(type: "int", nullable: false),
                    idApiSort = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiObjectId = table.Column<int>(type: "int", nullable: true),
                    ApiSortId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiObjectSort", x => new { x.idObject, x.idApiSort });
                    table.ForeignKey(
                        name: "FK_ApiObjectSort_Apis_ApiObjectId",
                        column: x => x.ApiObjectId,
                        principalTable: "Apis",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ApiObjectSort_ApiSortTypes_ApiSortId",
                        column: x => x.ApiSortId,
                        principalTable: "ApiSortTypes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiObjectFilter_ApiFilterId",
                table: "ApiObjectFilter",
                column: "ApiFilterId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiObjectFilter_ApiObjectId",
                table: "ApiObjectFilter",
                column: "ApiObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiObjectSort_ApiObjectId",
                table: "ApiObjectSort",
                column: "ApiObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiObjectSort_ApiSortId",
                table: "ApiObjectSort",
                column: "ApiSortId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiObjectFilter");

            migrationBuilder.DropTable(
                name: "ApiObjectSort");

            migrationBuilder.DropTable(
                name: "ApiFilterTypes");

            migrationBuilder.DropTable(
                name: "Apis");

            migrationBuilder.DropTable(
                name: "ApiSortTypes");
        }
    }
}
