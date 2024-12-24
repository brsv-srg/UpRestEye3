using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_ConsumerInfo_ConsumerId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_SupplierInfo_SupplierId",
                table: "Invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SupplierInfo",
                table: "SupplierInfo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConsumerInfo",
                table: "ConsumerInfo");

            migrationBuilder.RenameTable(
                name: "SupplierInfo",
                newName: "Suppliers");

            migrationBuilder.RenameTable(
                name: "ConsumerInfo",
                newName: "Consumers");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierInfo_TaxNumber",
                table: "Suppliers",
                newName: "IX_Suppliers_TaxNumber");

            migrationBuilder.RenameIndex(
                name: "IX_ConsumerInfo_TaxNumber",
                table: "Consumers",
                newName: "IX_Consumers_TaxNumber");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Consumers",
                table: "Consumers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Consumers_ConsumerId",
                table: "Invoices",
                column: "ConsumerId",
                principalTable: "Consumers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Suppliers_SupplierId",
                table: "Invoices",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Consumers_ConsumerId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Suppliers_SupplierId",
                table: "Invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Consumers",
                table: "Consumers");

            migrationBuilder.RenameTable(
                name: "Suppliers",
                newName: "SupplierInfo");

            migrationBuilder.RenameTable(
                name: "Consumers",
                newName: "ConsumerInfo");

            migrationBuilder.RenameIndex(
                name: "IX_Suppliers_TaxNumber",
                table: "SupplierInfo",
                newName: "IX_SupplierInfo_TaxNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Consumers_TaxNumber",
                table: "ConsumerInfo",
                newName: "IX_ConsumerInfo_TaxNumber");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SupplierInfo",
                table: "SupplierInfo",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConsumerInfo",
                table: "ConsumerInfo",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_ConsumerInfo_ConsumerId",
                table: "Invoices",
                column: "ConsumerId",
                principalTable: "ConsumerInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_SupplierInfo_SupplierId",
                table: "Invoices",
                column: "SupplierId",
                principalTable: "SupplierInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
