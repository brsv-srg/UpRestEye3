using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class RMSProd09 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RMSProductId",
                table: "RMSProducts",
                newName: "RMSProductExtGuid");

            migrationBuilder.RenameIndex(
                name: "IX_RMSProducts_ConsumerId_RMSProductId",
                table: "RMSProducts",
                newName: "IX_RMSProducts_ConsumerId_RMSProductExtGuid");

            migrationBuilder.RenameColumn(
                name: "RMSContainerId",
                table: "Containers",
                newName: "RMSContainerExtGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RMSProductExtGuid",
                table: "RMSProducts",
                newName: "RMSProductId");

            migrationBuilder.RenameIndex(
                name: "IX_RMSProducts_ConsumerId_RMSProductExtGuid",
                table: "RMSProducts",
                newName: "IX_RMSProducts_ConsumerId_RMSProductId");

            migrationBuilder.RenameColumn(
                name: "RMSContainerExtGuid",
                table: "Containers",
                newName: "RMSContainerId");
        }
    }
}
