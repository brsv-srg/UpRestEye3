using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class next11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_ConsumerId",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ConsumerId_SupplierId_InvoiceNumber",
                table: "Invoices",
                columns: new[] { "ConsumerId", "SupplierId", "InvoiceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_ConsumerId_SupplierId_InvoiceNumber",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ConsumerId",
                table: "Invoices",
                column: "ConsumerId");
        }
    }
}
