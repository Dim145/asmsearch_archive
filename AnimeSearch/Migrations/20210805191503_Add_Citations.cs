using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AnimeSearch.Migrations
{
    public partial class Add_Citations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Citations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    author_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    contenue = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    User_ID = table.Column<int>(type: "int", nullable: true),
                    is_validated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    date_ajout = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValue: DateTime.Now)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citations", x => x.id);
                    table.ForeignKey(
                        name: "FK_Citations_Users",
                        column: x => x.User_ID,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Citations_User_ID",
                table: "Citations",
                column: "User_ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Citations");
        }
    }
}
