using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AnimeSearch.Migrations
{
    public partial class Ajout_dernir_acces_admin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Dernier_Acces_Admin",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "date_ajout",
                table: "Citations",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValue: new DateTime(2021, 8, 31, 19, 11, 49, 93, DateTimeKind.Local).AddTicks(7415));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Dernier_Acces_Admin",
                table: "Users");

            migrationBuilder.AlterColumn<DateTime>(
                name: "date_ajout",
                table: "Citations",
                type: "datetime2",
                nullable: true,
                defaultValue: new DateTime(2021, 8, 31, 19, 11, 49, 93, DateTimeKind.Local).AddTicks(7415),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
