using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class next09 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Invoices",
                newName: "StageStatus");

            migrationBuilder.AddColumn<int>(
                name: "Stage",
                table: "Invoices",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stage",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "StageStatus",
                table: "Invoices",
                newName: "Status");
        }
    }
}
