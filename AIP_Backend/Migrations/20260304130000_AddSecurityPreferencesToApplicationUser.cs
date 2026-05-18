using AIPBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
	[DbContext(typeof(ApplicationDbContext))]
	[Migration("20260304130000_AddSecurityPreferencesToApplicationUser")]
	/// <inheritdoc />
	public partial class AddSecurityPreferencesToApplicationUser : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				IF COL_LENGTH('dbo.AspNetUsers', 'EmailNotificationsEnabled') IS NULL
					ALTER TABLE dbo.AspNetUsers ADD [EmailNotificationsEnabled] bit NOT NULL
						CONSTRAINT DF_AspNetUsers_EmailNotificationsEnabled DEFAULT (1);

				IF COL_LENGTH('dbo.AspNetUsers', 'LoginAlertsEnabled') IS NULL
					ALTER TABLE dbo.AspNetUsers ADD [LoginAlertsEnabled] bit NOT NULL
						CONSTRAINT DF_AspNetUsers_LoginAlertsEnabled DEFAULT (1);
				""");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				IF COL_LENGTH('dbo.AspNetUsers', 'EmailNotificationsEnabled') IS NOT NULL
				BEGIN
					DECLARE @dfEmail nvarchar(128);
					SELECT @dfEmail = dc.name
					FROM sys.default_constraints dc
					INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
					WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AspNetUsers')
					  AND c.name = N'EmailNotificationsEnabled';
					IF @dfEmail IS NOT NULL EXEC(N'ALTER TABLE dbo.AspNetUsers DROP CONSTRAINT ' + QUOTENAME(@dfEmail));
					ALTER TABLE dbo.AspNetUsers DROP COLUMN [EmailNotificationsEnabled];
				END

				IF COL_LENGTH('dbo.AspNetUsers', 'LoginAlertsEnabled') IS NOT NULL
				BEGIN
					DECLARE @dfLogin nvarchar(128);
					SELECT @dfLogin = dc.name
					FROM sys.default_constraints dc
					INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
					WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AspNetUsers')
					  AND c.name = N'LoginAlertsEnabled';
					IF @dfLogin IS NOT NULL EXEC(N'ALTER TABLE dbo.AspNetUsers DROP CONSTRAINT ' + QUOTENAME(@dfLogin));
					ALTER TABLE dbo.AspNetUsers DROP COLUMN [LoginAlertsEnabled];
				END
				""");
		}
	}
}
