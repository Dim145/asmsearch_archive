using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimeSearch.Migrations
{
    public partial class AddIsDeletableToSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeletable",
                table: "Settings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeletable",
                table: "Settings");
        }
    }
}
