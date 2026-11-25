using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShogaiBoard.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentMaintenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EquipmentMaintenances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EquipmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ScheduledStartAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ScheduledEndAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentMaintenances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentMaintenances_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentMaintenances_EquipmentId",
                table: "EquipmentMaintenances",
                column: "EquipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentMaintenances");
        }
    }
}
