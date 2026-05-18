using AIPBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
	[DbContext(typeof(ApplicationDbContext))]
	[Migration("20260518150000_SyncIncidentSchemaColumns")]
	/// <inheritdoc />
	public partial class SyncIncidentSchemaColumns : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// 20260305130208_RenameIncidentColumns was recorded as applied without running SQL on this database.
			migrationBuilder.Sql("""
				IF COL_LENGTH('dbo.Incidents', 'StoreName') IS NULL AND COL_LENGTH('dbo.Incidents', 'SiteName') IS NOT NULL
					EXEC sp_rename N'dbo.Incidents.SiteName', N'StoreName', N'COLUMN';

				IF COL_LENGTH('dbo.Incidents', 'StaffMemberName') IS NULL AND COL_LENGTH('dbo.Incidents', 'OfficerName') IS NOT NULL
					EXEC sp_rename N'dbo.Incidents.OfficerName', N'StaffMemberName', N'COLUMN';

				IF COL_LENGTH('dbo.Incidents', 'StaffMemberRole') IS NULL AND COL_LENGTH('dbo.Incidents', 'OfficerRole') IS NOT NULL
					EXEC sp_rename N'dbo.Incidents.OfficerRole', N'StaffMemberRole', N'COLUMN';

				IF COL_LENGTH('dbo.Incidents', 'IncidentCategory') IS NULL
					ALTER TABLE dbo.Incidents ADD [IncidentCategory] nvarchar(100) NULL;

				IF COL_LENGTH('dbo.Incidents', 'IncidentCategoryConfidence') IS NULL
					ALTER TABLE dbo.Incidents ADD [IncidentCategoryConfidence] float NULL;

				IF COL_LENGTH('dbo.Incidents', 'RiskLevel') IS NULL
					ALTER TABLE dbo.Incidents ADD [RiskLevel] nvarchar(20) NULL;

				IF COL_LENGTH('dbo.Incidents', 'RiskScore') IS NULL
					ALTER TABLE dbo.Incidents ADD [RiskScore] float NULL;

				IF COL_LENGTH('dbo.Incidents', 'ClassificationVersion') IS NULL
					ALTER TABLE dbo.Incidents ADD [ClassificationVersion] nvarchar(50) NULL;
				""");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				IF COL_LENGTH('dbo.Incidents', 'ClassificationVersion') IS NOT NULL
					ALTER TABLE dbo.Incidents DROP COLUMN [ClassificationVersion];
				IF COL_LENGTH('dbo.Incidents', 'RiskScore') IS NOT NULL
					ALTER TABLE dbo.Incidents DROP COLUMN [RiskScore];
				IF COL_LENGTH('dbo.Incidents', 'RiskLevel') IS NOT NULL
					ALTER TABLE dbo.Incidents DROP COLUMN [RiskLevel];
				IF COL_LENGTH('dbo.Incidents', 'IncidentCategoryConfidence') IS NOT NULL
					ALTER TABLE dbo.Incidents DROP COLUMN [IncidentCategoryConfidence];
				IF COL_LENGTH('dbo.Incidents', 'IncidentCategory') IS NOT NULL
					ALTER TABLE dbo.Incidents DROP COLUMN [IncidentCategory];

				IF COL_LENGTH('dbo.Incidents', 'OfficerRole') IS NULL AND COL_LENGTH('dbo.Incidents', 'StaffMemberRole') IS NOT NULL
					EXEC sp_rename N'dbo.Incidents.StaffMemberRole', N'OfficerRole', N'COLUMN';
				IF COL_LENGTH('dbo.Incidents', 'OfficerName') IS NULL AND COL_LENGTH('dbo.Incidents', 'StaffMemberName') IS NOT NULL
					EXEC sp_rename N'dbo.Incidents.StaffMemberName', N'OfficerName', N'COLUMN';
				IF COL_LENGTH('dbo.Incidents', 'SiteName') IS NULL AND COL_LENGTH('dbo.Incidents', 'StoreName') IS NOT NULL
					EXEC sp_rename N'dbo.Incidents.StoreName', N'SiteName', N'COLUMN';
				""");
		}
	}
}
