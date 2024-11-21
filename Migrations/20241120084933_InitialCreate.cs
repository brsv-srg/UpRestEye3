using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SupplierName = table.Column<string>(type: "TEXT", nullable: false),
                    SupplierTaxNumber = table.Column<string>(type: "TEXT", nullable: false),
                    SupplierBankAccount = table.Column<string>(type: "TEXT", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalAmountInclTaxes = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalAmountExclTaxes = table.Column<decimal>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExternalProductCode = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalProductName = table.Column<string>(type: "TEXT", nullable: false),
                    InternalProductCode = table.Column<string>(type: "TEXT", nullable: false),
                    InternalProductName = table.Column<string>(type: "TEXT", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalPriceInclTaxes = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExternalTaxCategory = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalTaxAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExternalPriceExclTaxes = table.Column<decimal>(type: "TEXT", nullable: false),
                    InternalPriceInclTaxes = table.Column<decimal>(type: "TEXT", nullable: false),
                    InternalTaxCategory = table.Column<string>(type: "TEXT", nullable: false),
                    InternalTaxAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    InternalPriceExclTaxes = table.Column<decimal>(type: "TEXT", nullable: false),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaxCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxCategories_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_InvoiceId",
                table: "Products",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCategories_InvoiceId",
                table: "TaxCategories",
                column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "TaxCategories");

            migrationBuilder.DropTable(
                name: "Invoices");
        }
    }
}
