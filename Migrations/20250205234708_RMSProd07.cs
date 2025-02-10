using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class RMSProd07 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountingCategory",
                table: "RMSProducts");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "RMSProducts");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "RMSProducts");

            migrationBuilder.DropColumn(
                name: "NotInStoreMovement",
                table: "RMSProducts");

            migrationBuilder.DropColumn(
                name: "Parent",
                table: "RMSProducts");

            migrationBuilder.DropColumn(
                name: "TaxCategory",
                table: "RMSProducts");

            migrationBuilder.DropColumn(
                name: "UnitCapacity",
                table: "RMSProducts");

            migrationBuilder.DropColumn(
                name: "UnitWeight",
                table: "RMSProducts");

            migrationBuilder.DropColumn(
                name: "BackwardRecalculation",
                table: "Containers");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "Containers");

            migrationBuilder.DropColumn(
                name: "MaxContainerWeight",
                table: "Containers");

            migrationBuilder.DropColumn(
                name: "MinContainerWeight",
                table: "Containers");

            migrationBuilder.DropColumn(
                name: "UseInFront",
                table: "Containers");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "TaxCategories",
                newName: "TaxCategory");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "InvoiceProducts",
                newName: "TaxCategory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TaxCategory",
                table: "TaxCategories",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "TaxCategory",
                table: "InvoiceProducts",
                newName: "Category");

            migrationBuilder.AddColumn<Guid>(
                name: "AccountingCategory",
                table: "RMSProducts",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Category",
                table: "RMSProducts",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "RMSProducts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotInStoreMovement",
                table: "RMSProducts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "Parent",
                table: "RMSProducts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaxCategory",
                table: "RMSProducts",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCapacity",
                table: "RMSProducts",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitWeight",
                table: "RMSProducts",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "BackwardRecalculation",
                table: "Containers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "Containers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxContainerWeight",
                table: "Containers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinContainerWeight",
                table: "Containers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "UseInFront",
                table: "Containers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
