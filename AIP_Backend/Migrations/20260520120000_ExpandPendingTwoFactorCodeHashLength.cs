using AIPBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
	[DbContext(typeof(ApplicationDbContext))]
	[Migration("20260520120000_ExpandPendingTwoFactorCodeHashLength")]
	/// <inheritdoc />
	public partial class ExpandPendingTwoFactorCodeHashLength : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<string>(
				name: "PendingTwoFactorCode",
				table: "AspNetUsers",
				type: "nvarchar(128)",
				maxLength: 128,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(12)",
				oldMaxLength: 12,
				oldNullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<string>(
				name: "PendingTwoFactorCode",
				table: "AspNetUsers",
				type: "nvarchar(12)",
				maxLength: 12,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(128)",
				oldMaxLength: 128,
				oldNullable: true);
		}
	}
}
