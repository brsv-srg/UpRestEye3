using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class next08 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnitsCount",
                table: "InvoiceProducts",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "QuantityOfContainers",
                table: "InvoiceProducts",
                newName: "Count");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "InvoiceProducts",
                newName: "UnitsCount");

            migrationBuilder.RenameColumn(
                name: "Count",
                table: "InvoiceProducts",
                newName: "QuantityOfContainers");
        }
    }
}
