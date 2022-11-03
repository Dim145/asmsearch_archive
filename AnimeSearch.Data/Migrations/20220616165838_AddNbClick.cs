using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimeSearch.Data.Migrations
{
    public partial class AddNbClick : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "nbClick",
                table: "Sites",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "nbClick",
                table: "Sites");
        }
    }
}
