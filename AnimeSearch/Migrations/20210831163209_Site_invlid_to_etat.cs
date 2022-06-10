using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AnimeSearch.Migrations
{
    public partial class Site_invlid_to_etat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isValidated",
                table: "Sites");

            migrationBuilder.AddColumn<int>(
                name: "etat",
                table: "Sites",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "date_ajout",
                table: "Citations",
                type: "datetime2",
                nullable: true,
                defaultValue: new DateTime(2021, 8, 31, 18, 32, 8, 966, DateTimeKind.Local).AddTicks(7767),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValue: new DateTime(2021, 8, 25, 20, 17, 10, 878, DateTimeKind.Local).AddTicks(4759));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "etat",
                table: "Sites");

            migrationBuilder.AddColumn<bool>(
                name: "isValidated",
                table: "Sites",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "date_ajout",
                table: "Citations",
                type: "datetime2",
                nullable: true,
                defaultValue: new DateTime(2021, 8, 25, 20, 17, 10, 878, DateTimeKind.Local).AddTicks(4759),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValue: new DateTime(2021, 8, 31, 18, 32, 8, 966, DateTimeKind.Local).AddTicks(7767));
        }
    }
}
