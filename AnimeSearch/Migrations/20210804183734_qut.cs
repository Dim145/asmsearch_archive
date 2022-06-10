using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AnimeSearch.Migrations
{
    public partial class qut : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    url = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    urlSearch = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    urlIcon = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    cheminBaliseA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    idBase = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    typeSite = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    is_internationnal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    isValidated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    postValues = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.url);
                    table.CheckConstraint("url_check", "([url] like 'http%.[a-z][a-z]%/')");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Navigateur = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Derniere_visite = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "IP",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Adresse_IP = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    User_ID = table.Column<int>(type: "int", nullable: false),
                    Derniere_utilisation = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IP", x => x.id);
                    table.ForeignKey(
                        name: "FK_IP_Users",
                        column: x => x.User_ID,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recherche",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    User_ID = table.Column<int>(type: "int", nullable: false),
                    recherche = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    nb_recherche = table.Column<int>(type: "int", nullable: false),
                    derniere_recherche = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recherche", x => x.id);
                    table.ForeignKey(
                        name: "FK_Recherche_Users",
                        column: x => x.User_ID,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IP_User_ID",
                table: "IP",
                column: "User_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Recherche_User_ID",
                table: "Recherche",
                column: "User_ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IP");

            migrationBuilder.DropTable(
                name: "Recherche");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
