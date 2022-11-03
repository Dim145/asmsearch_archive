using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AnimeSearch.Data.Migrations
{
    public partial class AddTypeSite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "urlIcon",
                table: "Sites",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "date_ajout",
                table: "Citations",
                type: "datetime2",
                nullable: true,
                defaultValue: new DateTime(2021, 8, 25, 20, 17, 10, 878, DateTimeKind.Local).AddTicks(4759),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValue: new DateTime(2021, 8, 15, 20, 1, 53, 861, DateTimeKind.Local).AddTicks(9252));

            migrationBuilder.CreateTable(
                name: "TypeSites",
                columns: table => new
                {
                    Type_Site = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypeSites", x => x.Type_Site);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TypeSites");

            migrationBuilder.AlterColumn<string>(
                name: "urlIcon",
                table: "Sites",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<DateTime>(
                name: "date_ajout",
                table: "Citations",
                type: "datetime2",
                nullable: true,
                defaultValue: new DateTime(2021, 8, 15, 20, 1, 53, 861, DateTimeKind.Local).AddTicks(9252),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValue: new DateTime(2021, 8, 25, 20, 17, 10, 878, DateTimeKind.Local).AddTicks(4759));
        }
    }
}
