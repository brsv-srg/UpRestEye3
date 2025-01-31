using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class RMSProd04 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceProducts_RMSProducts_RmsProductId",
                table: "InvoiceProducts");

            migrationBuilder.RenameColumn(
                name: "RmsProductId",
                table: "InvoiceProducts",
                newName: "RMSProductId");

            migrationBuilder.RenameIndex(
                name: "IX_InvoiceProducts_RmsProductId",
                table: "InvoiceProducts",
                newName: "IX_InvoiceProducts_RMSProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceProducts_RMSProducts_RMSProductId",
                table: "InvoiceProducts",
                column: "RMSProductId",
                principalTable: "RMSProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceProducts_RMSProducts_RMSProductId",
                table: "InvoiceProducts");

            migrationBuilder.RenameColumn(
                name: "RMSProductId",
                table: "InvoiceProducts",
                newName: "RmsProductId");

            migrationBuilder.RenameIndex(
                name: "IX_InvoiceProducts_RMSProductId",
                table: "InvoiceProducts",
                newName: "IX_InvoiceProducts_RmsProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceProducts_RMSProducts_RmsProductId",
                table: "InvoiceProducts",
                column: "RmsProductId",
                principalTable: "RMSProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
