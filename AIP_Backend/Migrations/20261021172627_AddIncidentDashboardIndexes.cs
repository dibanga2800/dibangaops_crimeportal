using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentDashboardIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StolenItems_IncidentId",
                table: "StolenItems");

            migrationBuilder.CreateIndex(
                name: "IX_StolenItems_IncidentId",
                table: "StolenItems",
                column: "IncidentId")
                .Annotation("SqlServer:Include", new[] { "TotalAmount", "RecoveredAmount" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Active_Date",
                table: "Incidents",
                columns: new[] { "RecordIsDeletedYN", "DateOfIncident" })
                .Annotation("SqlServer:Include", new[] { "CustomerId", "SiteId", "IncidentType", "Status", "Priority", "TotalStolenValue", "TotalRecoveredValue", "TotalLostValue", "TotalValueRecovered" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Customer_Active_Date",
                table: "Incidents",
                columns: new[] { "CustomerId", "RecordIsDeletedYN", "DateOfIncident" })
                .Annotation("SqlServer:Include", new[] { "SiteId", "IncidentType", "Status", "Priority", "TotalStolenValue", "TotalRecoveredValue", "TotalLostValue", "TotalValueRecovered" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StolenItems_IncidentId",
                table: "StolenItems");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_Active_Date",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_Customer_Active_Date",
                table: "Incidents");

            migrationBuilder.CreateIndex(
                name: "IX_StolenItems_IncidentId",
                table: "StolenItems",
                column: "IncidentId");
        }
    }
}
