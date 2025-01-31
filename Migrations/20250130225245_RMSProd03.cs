using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class RMSProd03 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContainerDAO_RMSProducts_RMSProductId",
                table: "ContainerDAO");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceProducts_ContainerDAO_RMSContainerId",
                table: "InvoiceProducts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContainerDAO",
                table: "ContainerDAO");

            migrationBuilder.RenameTable(
                name: "ContainerDAO",
                newName: "Containers");

            migrationBuilder.RenameIndex(
                name: "IX_ContainerDAO_RMSProductId",
                table: "Containers",
                newName: "IX_Containers_RMSProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Containers",
                table: "Containers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Containers_RMSProducts_RMSProductId",
                table: "Containers",
                column: "RMSProductId",
                principalTable: "RMSProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceProducts_Containers_RMSContainerId",
                table: "InvoiceProducts",
                column: "RMSContainerId",
                principalTable: "Containers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Containers_RMSProducts_RMSProductId",
                table: "Containers");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceProducts_Containers_RMSContainerId",
                table: "InvoiceProducts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Containers",
                table: "Containers");

            migrationBuilder.RenameTable(
                name: "Containers",
                newName: "ContainerDAO");

            migrationBuilder.RenameIndex(
                name: "IX_Containers_RMSProductId",
                table: "ContainerDAO",
                newName: "IX_ContainerDAO_RMSProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContainerDAO",
                table: "ContainerDAO",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ContainerDAO_RMSProducts_RMSProductId",
                table: "ContainerDAO",
                column: "RMSProductId",
                principalTable: "RMSProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceProducts_ContainerDAO_RMSContainerId",
                table: "InvoiceProducts",
                column: "RMSContainerId",
                principalTable: "ContainerDAO",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
