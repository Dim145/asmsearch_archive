using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimeSearch.Migrations
{
    public partial class AjoutTrakeurSearchPlateforme : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Source",
                table: "Recherche",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "Recherche");
        }
    }
}
