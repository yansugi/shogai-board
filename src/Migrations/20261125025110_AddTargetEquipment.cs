using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShogaiBoard.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EquipmentName",
                table: "EquipmentIssues");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "EquipmentIssues");

            migrationBuilder.AddColumn<int>(
                name: "EquipmentId",
                table: "EquipmentIssues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Equipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentIssues_EquipmentId",
                table: "EquipmentIssues",
                column: "EquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentIssues_Equipments_EquipmentId",
                table: "EquipmentIssues",
                column: "EquipmentId",
                principalTable: "Equipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentIssues_Equipments_EquipmentId",
                table: "EquipmentIssues");

            migrationBuilder.DropTable(
                name: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentIssues_EquipmentId",
                table: "EquipmentIssues");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "EquipmentIssues");

            migrationBuilder.AddColumn<string>(
                name: "EquipmentName",
                table: "EquipmentIssues",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "EquipmentIssues",
                type: "TEXT",
                nullable: true);
        }
    }
}
