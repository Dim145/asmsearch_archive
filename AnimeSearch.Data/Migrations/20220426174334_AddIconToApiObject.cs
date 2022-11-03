using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimeSearch.Data.Migrations
{
    public partial class AddIconToApiObject : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "iconUrl",
                table: "Apis",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "iconUrl",
                table: "Apis");
        }
    }
}
