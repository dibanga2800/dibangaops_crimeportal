using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRecoveredItemTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RecoveredAmount",
                table: "StolenItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RecoveredQuantity",
                table: "StolenItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "WasRecovered",
                table: "StolenItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalLostValue",
                table: "Incidents",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalRecoveredQuantity",
                table: "Incidents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalRecoveredValue",
                table: "Incidents",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalStolenValue",
                table: "Incidents",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecoveredAmount",
                table: "StolenItems");

            migrationBuilder.DropColumn(
                name: "RecoveredQuantity",
                table: "StolenItems");

            migrationBuilder.DropColumn(
                name: "WasRecovered",
                table: "StolenItems");

            migrationBuilder.DropColumn(
                name: "TotalLostValue",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "TotalRecoveredQuantity",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "TotalRecoveredValue",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "TotalStolenValue",
                table: "Incidents");
        }
    }
}
