using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class next04 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RMSStorageId",
                table: "InvoiceProducts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceProducts_RMSStorageId",
                table: "InvoiceProducts",
                column: "RMSStorageId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceProducts_Accounts_RMSStorageId",
                table: "InvoiceProducts",
                column: "RMSStorageId",
                principalTable: "Accounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceProducts_Accounts_RMSStorageId",
                table: "InvoiceProducts");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceProducts_RMSStorageId",
                table: "InvoiceProducts");

            migrationBuilder.DropColumn(
                name: "RMSStorageId",
                table: "InvoiceProducts");
        }
    }
}
