using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class RestructureDailyActivityReportsRemoveJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivitiesJson",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "ComplianceJson",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "IncidentsJson",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "InsecureAreasJson",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "SecurityChecksJson",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "SystemsNotWorkingJson",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "VisitorLogJson",
                table: "DailyActivityReports");

            // Azure / fresh DBs may never have had UserCompany (e.g. already replaced by CustomerId).
            migrationBuilder.Sql(@"
DECLARE @dc sysname;
SELECT @dc = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE [d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'UserCompany';
IF @dc IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @dc + N'];');
IF COL_LENGTH(N'dbo.AspNetUsers', N'UserCompany') IS NOT NULL
    ALTER TABLE [AspNetUsers] DROP COLUMN [UserCompany];
");

            migrationBuilder.AddColumn<string>(
                name: "AtmAbuse",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AtmAbuseDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeSafeBSecurePoster",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeSafeBSecurePosterDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyWornCctv",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyWornCctvDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarParkGrounds",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarParkGroundsDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CashOfficeDoorOpen",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CashOfficeDoorOpenDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cctv",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CctvDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CigaretteTracker",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CigaretteTrackerDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CrimeReporting",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CrimeReportingDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FireDoorsBackOfHouse",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FireDoorsBackOfHouseDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FireDoorsShopFloor",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FireDoorsShopFloorDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FireRoutesBlocked",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FireRoutesBlockedDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HighValueRoom",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HighValueRoomDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntruderAlarm",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntruderAlarmDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Keyholding",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyholdingDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KioskSecure",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KioskSecureDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagersOffice",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagersOfficeDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceYard",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceYardDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TillsContainedOverCash",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TillsContainedOverCashDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisibleCashOnDisplay",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisibleCashOnDisplayDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisibleKeysOnDisplay",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisibleKeysOnDisplayDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarehouseToSalesFloor",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarehouseToSalesFloorDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WatchMeNow",
                table: "DailyActivityReports",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WatchMeNowDescription",
                table: "DailyActivityReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DailyActivityReportActivities",
                columns: table => new
                {
                    DailyActivityReportActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyActivityReportId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Time = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Activity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyActivityReportActivities", x => x.DailyActivityReportActivityId);
                    table.ForeignKey(
                        name: "FK_DailyActivityReportActivities_DailyActivityReports_DailyActivityReportId",
                        column: x => x.DailyActivityReportId,
                        principalTable: "DailyActivityReports",
                        principalColumn: "DailyActivityReportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyActivityReportIncidents",
                columns: table => new
                {
                    DailyActivityReportIncidentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyActivityReportId = table.Column<int>(type: "int", nullable: false),
                    IncidentId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Time = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActionTaken = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Resolved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyActivityReportIncidents", x => x.DailyActivityReportIncidentId);
                    table.ForeignKey(
                        name: "FK_DailyActivityReportIncidents_DailyActivityReports_DailyActivityReportId",
                        column: x => x.DailyActivityReportId,
                        principalTable: "DailyActivityReports",
                        principalColumn: "DailyActivityReportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyActivityReportSecurityChecks",
                columns: table => new
                {
                    DailyActivityReportSecurityCheckId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyActivityReportId = table.Column<int>(type: "int", nullable: false),
                    SecurityCheckId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Time = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Area = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CheckType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyActivityReportSecurityChecks", x => x.DailyActivityReportSecurityCheckId);
                    table.ForeignKey(
                        name: "FK_DailyActivityReportSecurityChecks_DailyActivityReports_DailyActivityReportId",
                        column: x => x.DailyActivityReportId,
                        principalTable: "DailyActivityReports",
                        principalColumn: "DailyActivityReportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyActivityReportVisitorEntries",
                columns: table => new
                {
                    DailyActivityReportVisitorEntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyActivityReportId = table.Column<int>(type: "int", nullable: false),
                    VisitorEntryId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Time = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VisitorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Company = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EscortedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExitTime = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BadgeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyActivityReportVisitorEntries", x => x.DailyActivityReportVisitorEntryId);
                    table.ForeignKey(
                        name: "FK_DailyActivityReportVisitorEntries_DailyActivityReports_DailyActivityReportId",
                        column: x => x.DailyActivityReportId,
                        principalTable: "DailyActivityReports",
                        principalColumn: "DailyActivityReportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyActivityReportActivities_DailyActivityReportId",
                table: "DailyActivityReportActivities",
                column: "DailyActivityReportId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyActivityReportIncidents_DailyActivityReportId",
                table: "DailyActivityReportIncidents",
                column: "DailyActivityReportId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyActivityReportSecurityChecks_DailyActivityReportId",
                table: "DailyActivityReportSecurityChecks",
                column: "DailyActivityReportId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyActivityReportVisitorEntries_DailyActivityReportId",
                table: "DailyActivityReportVisitorEntries",
                column: "DailyActivityReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyActivityReportActivities");

            migrationBuilder.DropTable(
                name: "DailyActivityReportIncidents");

            migrationBuilder.DropTable(
                name: "DailyActivityReportSecurityChecks");

            migrationBuilder.DropTable(
                name: "DailyActivityReportVisitorEntries");

            migrationBuilder.DropColumn(
                name: "AtmAbuse",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "AtmAbuseDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "BeSafeBSecurePoster",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "BeSafeBSecurePosterDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "BodyWornCctv",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "BodyWornCctvDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "CarParkGrounds",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "CarParkGroundsDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "CashOfficeDoorOpen",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "CashOfficeDoorOpenDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "Cctv",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "CctvDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "CigaretteTracker",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "CigaretteTrackerDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "CrimeReporting",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "CrimeReportingDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "FireDoorsBackOfHouse",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "FireDoorsBackOfHouseDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "FireDoorsShopFloor",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "FireDoorsShopFloorDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "FireRoutesBlocked",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "FireRoutesBlockedDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "HighValueRoom",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "HighValueRoomDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "IntruderAlarm",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "IntruderAlarmDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "Keyholding",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "KeyholdingDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "KioskSecure",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "KioskSecureDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "ManagersOffice",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "ManagersOfficeDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "ServiceYard",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "ServiceYardDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "TillsContainedOverCash",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "TillsContainedOverCashDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "VisibleCashOnDisplay",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "VisibleCashOnDisplayDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "VisibleKeysOnDisplay",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "VisibleKeysOnDisplayDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "WarehouseToSalesFloor",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "WarehouseToSalesFloorDescription",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "WatchMeNow",
                table: "DailyActivityReports");

            migrationBuilder.DropColumn(
                name: "WatchMeNowDescription",
                table: "DailyActivityReports");

            migrationBuilder.AddColumn<string>(
                name: "ActivitiesJson",
                table: "DailyActivityReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComplianceJson",
                table: "DailyActivityReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IncidentsJson",
                table: "DailyActivityReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsecureAreasJson",
                table: "DailyActivityReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityChecksJson",
                table: "DailyActivityReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemsNotWorkingJson",
                table: "DailyActivityReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisitorLogJson",
                table: "DailyActivityReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserCompany",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
