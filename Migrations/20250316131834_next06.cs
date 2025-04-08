using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class next06 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "InvoiceProducts");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "InvoiceProducts",
                newName: "UnitsCount");

            migrationBuilder.AddColumn<string>(
                name: "Container",
                table: "InvoiceProducts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ProductTotalValue",
                table: "InvoiceProducts",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityOfContainers",
                table: "InvoiceProducts",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Container",
                table: "InvoiceProducts");

            migrationBuilder.DropColumn(
                name: "ProductTotalValue",
                table: "InvoiceProducts");

            migrationBuilder.DropColumn(
                name: "QuantityOfContainers",
                table: "InvoiceProducts");

            migrationBuilder.RenameColumn(
                name: "UnitsCount",
                table: "InvoiceProducts",
                newName: "Price");

            migrationBuilder.AddColumn<float>(
                name: "Quantity",
                table: "InvoiceProducts",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
