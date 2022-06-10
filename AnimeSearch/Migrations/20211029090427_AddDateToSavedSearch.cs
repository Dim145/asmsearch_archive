using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AnimeSearch.Migrations
{
    public partial class AddDateToSavedSearch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Date_Sauvegarde",
                table: "SavedSearch",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSDATETIME()");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date_Sauvegarde",
                table: "SavedSearch");
        }
    }
}
