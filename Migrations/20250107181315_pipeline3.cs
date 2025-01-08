using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class pipeline3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxCategories_Invoices_InvoiceId",
                table: "TaxCategories");

            migrationBuilder.AlterColumn<int>(
                name: "InvoiceId",
                table: "TaxCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxCategories_Invoices_InvoiceId",
                table: "TaxCategories",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxCategories_Invoices_InvoiceId",
                table: "TaxCategories");

            migrationBuilder.AlterColumn<int>(
                name: "InvoiceId",
                table: "TaxCategories",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_TaxCategories_Invoices_InvoiceId",
                table: "TaxCategories",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id");
        }
    }
}
