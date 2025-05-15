using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class next18 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Parent",
                table: "RMSProducts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Parent",
                table: "RMSProducts");
        }
    }
}
