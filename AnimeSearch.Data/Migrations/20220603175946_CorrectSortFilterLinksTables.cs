using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimeSearch.Data.Migrations
{
    public partial class CorrectSortFilterLinksTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiObjectFilter_ApiFilterTypes_ApiFilterId",
                table: "ApiObjectFilter");

            migrationBuilder.DropForeignKey(
                name: "FK_ApiObjectFilter_Apis_ApiObjectId",
                table: "ApiObjectFilter");

            migrationBuilder.DropForeignKey(
                name: "FK_ApiObjectSort_Apis_ApiObjectId",
                table: "ApiObjectSort");

            migrationBuilder.DropForeignKey(
                name: "FK_ApiObjectSort_ApiSortTypes_ApiSortId",
                table: "ApiObjectSort");

            migrationBuilder.DropIndex(
                name: "IX_ApiObjectSort_ApiObjectId",
                table: "ApiObjectSort");

            migrationBuilder.DropIndex(
                name: "IX_ApiObjectSort_ApiSortId",
                table: "ApiObjectSort");

            migrationBuilder.DropIndex(
                name: "IX_ApiObjectFilter_ApiFilterId",
                table: "ApiObjectFilter");

            migrationBuilder.DropIndex(
                name: "IX_ApiObjectFilter_ApiObjectId",
                table: "ApiObjectFilter");

            migrationBuilder.DropColumn(
                name: "ApiObjectId",
                table: "ApiObjectSort");

            migrationBuilder.DropColumn(
                name: "ApiSortId",
                table: "ApiObjectSort");

            migrationBuilder.DropColumn(
                name: "ApiFilterId",
                table: "ApiObjectFilter");

            migrationBuilder.DropColumn(
                name: "ApiObjectId",
                table: "ApiObjectFilter");

            migrationBuilder.CreateIndex(
                name: "IX_ApiObjectSort_idApiSort",
                table: "ApiObjectSort",
                column: "idApiSort");

            migrationBuilder.CreateIndex(
                name: "IX_ApiObjectFilter_idApiFilter",
                table: "ApiObjectFilter",
                column: "idApiFilter");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiObjectFilter_ApiFilterTypes_idApiFilter",
                table: "ApiObjectFilter",
                column: "idApiFilter",
                principalTable: "ApiFilterTypes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApiObjectFilter_Apis_idObject",
                table: "ApiObjectFilter",
                column: "idObject",
                principalTable: "Apis",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApiObjectSort_Apis_idObject",
                table: "ApiObjectSort",
                column: "idObject",
                principalTable: "Apis",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApiObjectSort_ApiSortTypes_idApiSort",
                table: "ApiObjectSort",
                column: "idApiSort",
                principalTable: "ApiSortTypes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiObjectFilter_ApiFilterTypes_idApiFilter",
                table: "ApiObjectFilter");

            migrationBuilder.DropForeignKey(
                name: "FK_ApiObjectFilter_Apis_idObject",
                table: "ApiObjectFilter");

            migrationBuilder.DropForeignKey(
                name: "FK_ApiObjectSort_Apis_idObject",
                table: "ApiObjectSort");

            migrationBuilder.DropForeignKey(
                name: "FK_ApiObjectSort_ApiSortTypes_idApiSort",
                table: "ApiObjectSort");

            migrationBuilder.DropIndex(
                name: "IX_ApiObjectSort_idApiSort",
                table: "ApiObjectSort");

            migrationBuilder.DropIndex(
                name: "IX_ApiObjectFilter_idApiFilter",
                table: "ApiObjectFilter");

            migrationBuilder.AddColumn<int>(
                name: "ApiObjectId",
                table: "ApiObjectSort",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApiSortId",
                table: "ApiObjectSort",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApiFilterId",
                table: "ApiObjectFilter",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApiObjectId",
                table: "ApiObjectFilter",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiObjectSort_ApiObjectId",
                table: "ApiObjectSort",
                column: "ApiObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiObjectSort_ApiSortId",
                table: "ApiObjectSort",
                column: "ApiSortId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiObjectFilter_ApiFilterId",
                table: "ApiObjectFilter",
                column: "ApiFilterId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiObjectFilter_ApiObjectId",
                table: "ApiObjectFilter",
                column: "ApiObjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiObjectFilter_ApiFilterTypes_ApiFilterId",
                table: "ApiObjectFilter",
                column: "ApiFilterId",
                principalTable: "ApiFilterTypes",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiObjectFilter_Apis_ApiObjectId",
                table: "ApiObjectFilter",
                column: "ApiObjectId",
                principalTable: "Apis",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiObjectSort_Apis_ApiObjectId",
                table: "ApiObjectSort",
                column: "ApiObjectId",
                principalTable: "Apis",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiObjectSort_ApiSortTypes_ApiSortId",
                table: "ApiObjectSort",
                column: "ApiSortId",
                principalTable: "ApiSortTypes",
                principalColumn: "id");
        }
    }
}
