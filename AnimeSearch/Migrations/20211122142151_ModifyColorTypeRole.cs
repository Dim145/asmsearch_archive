using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimeSearch.Migrations
{
    public partial class ModifyColorTypeRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "0",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Color",
                table: "Roles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "0");
        }
    }
}
