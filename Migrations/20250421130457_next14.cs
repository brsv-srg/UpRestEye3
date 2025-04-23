using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class next14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConnectionParameters_RMSProducts_ConsumerId",
                table: "ConnectionParameters");

            migrationBuilder.AddColumn<bool>(
                name: "ProductsTaxIncluded",
                table: "Invoices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionParameters_DeliveryServiceId",
                table: "ConnectionParameters",
                column: "DeliveryServiceId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectionParameters_RMSProducts_DeliveryServiceId",
                table: "ConnectionParameters",
                column: "DeliveryServiceId",
                principalTable: "RMSProducts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConnectionParameters_RMSProducts_DeliveryServiceId",
                table: "ConnectionParameters");

            migrationBuilder.DropIndex(
                name: "IX_ConnectionParameters_DeliveryServiceId",
                table: "ConnectionParameters");

            migrationBuilder.DropColumn(
                name: "ProductsTaxIncluded",
                table: "Invoices");

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectionParameters_RMSProducts_ConsumerId",
                table: "ConnectionParameters",
                column: "ConsumerId",
                principalTable: "RMSProducts",
                principalColumn: "Id");
        }
    }
}
