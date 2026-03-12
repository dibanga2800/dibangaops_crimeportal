using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class RenameIncidentJsonColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WitnessStatementsJson",
                table: "Incidents",
                newName: "WitnessStatements");

            migrationBuilder.RenameColumn(
                name: "InvolvedPartiesJson",
                table: "Incidents",
                newName: "InvolvedParties");

            migrationBuilder.RenameColumn(
                name: "IncidentInvolvedJson",
                table: "Incidents",
                newName: "IncidentInvolved");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WitnessStatements",
                table: "Incidents",
                newName: "WitnessStatementsJson");

            migrationBuilder.RenameColumn(
                name: "InvolvedParties",
                table: "Incidents",
                newName: "InvolvedPartiesJson");

            migrationBuilder.RenameColumn(
                name: "IncidentInvolved",
                table: "Incidents",
                newName: "IncidentInvolvedJson");
        }
    }
}
