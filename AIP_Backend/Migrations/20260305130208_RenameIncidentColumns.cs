using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class RenameIncidentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Columns already renamed via deployment/sql/12-rename-incident-columns.sql
            // No-op: EF records this migration as applied without touching the DB
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StoreName",
                table: "Incidents",
                newName: "SiteName");

            migrationBuilder.RenameColumn(
                name: "StaffMemberRole",
                table: "Incidents",
                newName: "OfficerRole");

            migrationBuilder.RenameColumn(
                name: "StaffMemberName",
                table: "Incidents",
                newName: "OfficerName");
        }
    }
}
