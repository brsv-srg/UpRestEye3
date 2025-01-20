using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateRef4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsumerId",
                table: "Suppliers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ConnectionParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApiUrl = table.Column<string>(type: "TEXT", nullable: false),
                    ApiLogin = table.Column<string>(type: "TEXT", nullable: false),
                    ApiPassword = table.Column<string>(type: "TEXT", nullable: false),
                    ConsumerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionParameters_Consumers_ConsumerId",
                        column: x => x.ConsumerId,
                        principalTable: "Consumers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Login = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    ConsumerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Consumers_ConsumerId",
                        column: x => x.ConsumerId,
                        principalTable: "Consumers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_ConsumerId",
                table: "Suppliers",
                column: "ConsumerId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionParameters_ConsumerId",
                table: "ConnectionParameters",
                column: "ConsumerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ConsumerId",
                table: "Users",
                column: "ConsumerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Consumers_ConsumerId",
                table: "Suppliers",
                column: "ConsumerId",
                principalTable: "Consumers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Consumers_ConsumerId",
                table: "Suppliers");

            migrationBuilder.DropTable(
                name: "ConnectionParameters");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_ConsumerId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ConsumerId",
                table: "Suppliers");
        }
    }
}
