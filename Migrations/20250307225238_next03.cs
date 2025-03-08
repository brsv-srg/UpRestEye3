using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpRestEye3.Migrations
{
    /// <inheritdoc />
    public partial class next03 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Удаление таблицы MeasureUnits
            migrationBuilder.DropTable(name: "MeasureUnits");

            // Создание таблицы MeasureUnits заново с изменениями
            migrationBuilder.CreateTable(
                name: "MeasureUnits",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConsumerId = table.Column<int>(nullable: true),
                    EntityExtGuid = table.Column<Guid>(nullable: false),
                    RootType = table.Column<string>(nullable: false),
                    Code = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false)

                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasureUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasureUnits_Consumers_ConsumerId",
                        column: x => x.ConsumerId,
                        principalTable: "Consumers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Восстановление индексов, если они были
            migrationBuilder.CreateIndex(
                name: "IX_MeasureUnits_ConsumerId",
                table: "MeasureUnits",
                column: "ConsumerId");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Удаление таблицы MeasureUnits
            migrationBuilder.DropTable(name: "MeasureUnits");

            // Создание таблицы MeasureUnits заново с изменениями
            migrationBuilder.CreateTable(
                name: "MeasureUnits",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConsumerId = table.Column<int>(nullable: true),
                    EntityExtGuid = table.Column<Guid>(nullable: false),
                    RootType = table.Column<string>(nullable: false),
                    Code = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false)

                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasureUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasureUnits_Consumers_ConsumerId",
                        column: x => x.ConsumerId,
                        principalTable: "Consumers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Восстановление индексов, если они были
            migrationBuilder.CreateIndex(
                name: "IX_MeasureUnits_ConsumerId",
                table: "MeasureUnits",
                column: "ConsumerId");

        }
    }
}
