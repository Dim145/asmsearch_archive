using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimeSearch.Data.Migrations
{
    public partial class AddCurrentCitation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_current",
                table: "Citations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_current",
                table: "Citations");
        }
    }
}
