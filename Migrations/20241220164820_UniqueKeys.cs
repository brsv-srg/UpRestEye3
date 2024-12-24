using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class UniqueKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SupplierInfo_TaxNumber",
                table: "SupplierInfo",
                column: "TaxNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerInfo_TaxNumber",
                table: "ConsumerInfo",
                column: "TaxNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierInfo_TaxNumber",
                table: "SupplierInfo");

            migrationBuilder.DropIndex(
                name: "IX_ConsumerInfo_TaxNumber",
                table: "ConsumerInfo");
        }
    }
}
