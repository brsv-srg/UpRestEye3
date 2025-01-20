using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class users3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suppliers_TaxNumber",
                table: "Suppliers");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TaxNumber_ConsumerId",
                table: "Suppliers",
                columns: new[] { "TaxNumber", "ConsumerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suppliers_TaxNumber_ConsumerId",
                table: "Suppliers");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TaxNumber",
                table: "Suppliers",
                column: "TaxNumber",
                unique: true);
        }
    }
}
