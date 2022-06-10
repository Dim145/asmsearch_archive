using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AnimeSearch.Migrations
{
    public partial class update_site_etat_type : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte>(
                name: "etat",
                table: "Sites",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "date_ajout",
                table: "Citations",
                type: "datetime2",
                nullable: true,
                defaultValue: new DateTime(2021, 8, 31, 19, 11, 49, 93, DateTimeKind.Local).AddTicks(7415),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValue: new DateTime(2021, 8, 31, 18, 32, 8, 966, DateTimeKind.Local).AddTicks(7767));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "etat",
                table: "Sites",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldDefaultValue: (byte)0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "date_ajout",
                table: "Citations",
                type: "datetime2",
                nullable: true,
                defaultValue: new DateTime(2021, 8, 31, 18, 32, 8, 966, DateTimeKind.Local).AddTicks(7767),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValue: new DateTime(2021, 8, 31, 19, 11, 49, 93, DateTimeKind.Local).AddTicks(7415));
        }
    }
}
