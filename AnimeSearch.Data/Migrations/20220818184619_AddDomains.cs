using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimeSearch.Data.Migrations
{
    public partial class AddDomains : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Domains",
                columns: table => new
                {
                    url = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    desc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    last_seen = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Domains", x => x.url);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Domains");
        }
    }
}
