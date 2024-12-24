using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class migr9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Consumers_ConsumerInfoId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Suppliers_SupplierInfoId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ConsumerInfoId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_SupplierInfoId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ConsumerInfoId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SupplierInfoId",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ConsumerId",
                table: "Invoices",
                column: "ConsumerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SupplierId",
                table: "Invoices",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Consumers_ConsumerId",
                table: "Invoices",
                column: "ConsumerId",
                principalTable: "Consumers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Suppliers_SupplierId",
                table: "Invoices",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ConsumerId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_SupplierId",
                table: "Invoices");

            migrationBuilder.AddColumn<int>(
                name: "ConsumerInfoId",
                table: "Invoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierInfoId",
                table: "Invoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ConsumerInfoId",
                table: "Invoices",
                column: "ConsumerInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SupplierInfoId",
                table: "Invoices",
                column: "SupplierInfoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Consumers_ConsumerInfoId",
                table: "Invoices",
                column: "ConsumerInfoId",
                principalTable: "Consumers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Suppliers_SupplierInfoId",
                table: "Invoices",
                column: "SupplierInfoId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }
    }
}
