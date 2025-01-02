using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateRef1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "TaxCategories",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "Info_TotalTax",
                table: "Invoices",
                newName: "Info_TotalIVA");

            migrationBuilder.AlterColumn<int>(
                name: "Category",
                table: "TaxCategories",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<decimal>(
                name: "Base",
                table: "TaxCategories",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IVA",
                table: "TaxCategories",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Invoices",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Base",
                table: "TaxCategories");

            migrationBuilder.DropColumn(
                name: "IVA",
                table: "TaxCategories");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "TaxCategories",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "Info_TotalIVA",
                table: "Invoices",
                newName: "Info_TotalTax");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "TaxCategories",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Invoices",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }
    }
}
