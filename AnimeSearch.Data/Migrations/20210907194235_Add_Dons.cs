using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AnimeSearch.Data.Migrations
{
    public partial class Add_Dons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    amout = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    done = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    User_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dons", x => x.id);
                    table.ForeignKey(
                        name: "FK_Dons_Users",
                        column: x => x.User_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dons_User_id",
                table: "Dons",
                column: "User_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dons");
        }
    }
}
