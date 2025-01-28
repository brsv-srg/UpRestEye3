using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class RMSProd01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suppliers_ConsumerId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_TaxNumber_ConsumerId",
                table: "Suppliers");

            migrationBuilder.CreateTable(
                name: "RMSProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConsumerId = table.Column<int>(type: "INTEGER", nullable: false),
                    RMSProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Num = table.Column<string>(type: "TEXT", nullable: false),
                    Parent = table.Column<Guid>(type: "TEXT", nullable: true),
                    TaxCategory = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountingCategory = table.Column<Guid>(type: "TEXT", nullable: false),
                    MainUnit = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitWeight = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitCapacity = table.Column<decimal>(type: "TEXT", nullable: false),
                    NotInStoreMovement = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RMSProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RMSProducts_Consumers_ConsumerId",
                        column: x => x.ConsumerId,
                        principalTable: "Consumers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Containers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RMSContainerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Num = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Count = table.Column<decimal>(type: "TEXT", nullable: false),
                    MinContainerWeight = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxContainerWeight = table.Column<decimal>(type: "TEXT", nullable: false),
                    ContainerWeight = table.Column<decimal>(type: "TEXT", nullable: false),
                    FullContainerWeight = table.Column<decimal>(type: "TEXT", nullable: false),
                    BackwardRecalculation = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseInFront = table.Column<bool>(type: "INTEGER", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Containers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Containers_RMSProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "RMSProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_ConsumerId_TaxNumber",
                table: "Suppliers",
                columns: new[] { "ConsumerId", "TaxNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Containers_ProductId",
                table: "Containers",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RMSProducts_ConsumerId_RMSProductId",
                table: "RMSProducts",
                columns: new[] { "ConsumerId", "RMSProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Containers");

            migrationBuilder.DropTable(
                name: "RMSProducts");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_ConsumerId_TaxNumber",
                table: "Suppliers");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_ConsumerId",
                table: "Suppliers",
                column: "ConsumerId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TaxNumber_ConsumerId",
                table: "Suppliers",
                columns: new[] { "TaxNumber", "ConsumerId" },
                unique: true);
        }
    }
}
