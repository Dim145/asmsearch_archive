using Microsoft.EntityFrameworkCore.Migrations;

namespace AnimeSearch.Data.Migrations
{
    public partial class Update_IP : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Localisation",
                table: "IP",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Localisation",
                table: "IP");
        }
    }
}
