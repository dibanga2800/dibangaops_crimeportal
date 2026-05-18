using AIPBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
	[DbContext(typeof(ApplicationDbContext))]
	[Migration("20260518120000_PurgeStalePageAccessRows")]
	/// <inheritdoc />
	public partial class PurgeStalePageAccessRows : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql(
				"""
				DECLARE @StalePageIds TABLE (Id INT PRIMARY KEY);

				INSERT INTO @StalePageIds (Id)
				SELECT pa.Id
				FROM [dbo].[PageAccesses] AS pa
				WHERE LOWER(LTRIM(RTRIM(pa.PageId))) NOT IN (
					'dashboard',
					'profile',
					'settings',
					'alert-rules',
					'data-analytics-hub',
					'user-setup',
					'employee-registration',
					'customer-setup',
					'barcode-catalog-import',
					'product-catalog',
					'incident-report',
					'incident-graph',
					'crime-intelligence',
					'customer-incident-report',
					'customer-incident-graph',
					'customer-crime-intelligence'
				)
				AND (
					LOWER(LTRIM(RTRIM(pa.PageId))) IN (
						'action-calendar',
						'bank-holiday',
						'bank-holidays',
						'holiday-request',
						'holiday-requests',
						'daily-occurrence-book',
						'daily-occurrence',
						'occurrence-book',
						'mystery-shopper',
						'site-visit',
						'officer-support',
						'manager-support',
						'safe-duress',
						'safe-duress-words',
						'crm',
						'contacts',
						'contacts-crm',
						'management-customer-reporting',
						'customer-reporting-page',
						'customer-reporting',
						'customer-satisfaction-report',
						'satisfaction-report',
						'customer-views-config',
						'be-safe-be-secure',
						'incident-list',
						'stock-management',
						'employee-activity'
					)
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE 'compliance-%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE 'management-%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE 'recruitment-%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE '%holiday%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE '%bank-holiday%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE '%occurrence%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE '%mystery-shopper%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE '%site-visit%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE '%officer-support%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE '%manager-support%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE '%safe-duress%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE '%customer-reporting%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE '%customer-satisfaction%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE '%satisfaction-report%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE '%action-calendar%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE '%incident-list%'
					OR LOWER(LTRIM(RTRIM(pa.PageId))) LIKE '%employee-activity%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '/compliance/%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '/management/%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '/recruitment/%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '/crm%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) = '/contacts'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '/contacts/%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/action-calendar%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/holiday%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/bank-holiday%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/mystery-shopper%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/site-visit%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/daily-occurrence%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/occurrence-book%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/officer-support%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/manager-support%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/safe-duress%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/customer-reporting%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/customer-satisfaction%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/satisfaction-report%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/incident-list%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/employee-activity%'
					OR LOWER(LTRIM(RTRIM(ISNULL(pa.Path, '')))) LIKE '%/stock-management%'
				);

				DELETE rpa
				FROM [dbo].[RolePageAccesses] AS rpa
				INNER JOIN @StalePageIds AS stale ON stale.Id = rpa.PageAccessId;

				DELETE cpa
				FROM [dbo].[CustomerPageAccesses] AS cpa
				INNER JOIN @StalePageIds AS stale ON stale.Id = cpa.PageAccessId;

				DELETE pa
				FROM [dbo].[PageAccesses] AS pa
				INNER JOIN @StalePageIds AS stale ON stale.Id = pa.Id;
				""");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			// Purged page-access rows are not recreated.
		}
	}
}
