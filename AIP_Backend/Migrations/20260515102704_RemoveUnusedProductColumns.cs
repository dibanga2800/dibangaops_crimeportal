using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedProductColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Production may already match target schema (manual SQL / drift). Keep idempotent.
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes i
                    WHERE i.name = N'IX_Products_Category'
                      AND i.object_id = OBJECT_ID(N'dbo.Products'))
                    DROP INDEX IX_Products_Category ON dbo.Products;

                IF COL_LENGTH('dbo.Products', 'Brand') IS NOT NULL
                    ALTER TABLE dbo.Products DROP COLUMN Brand;
                IF COL_LENGTH('dbo.Products', 'Category') IS NOT NULL
                    ALTER TABLE dbo.Products DROP COLUMN Category;
                IF COL_LENGTH('dbo.Products', 'Manufacturer') IS NOT NULL
                    ALTER TABLE dbo.Products DROP COLUMN Manufacturer;
                IF COL_LENGTH('dbo.Products', 'Section') IS NOT NULL
                    ALTER TABLE dbo.Products DROP COLUMN Section;
                IF COL_LENGTH('dbo.Products', 'L8Name') IS NOT NULL
                    ALTER TABLE dbo.Products DROP COLUMN L8Name;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");

            migrationBuilder.AddColumn<string>(
                name: "L8Name",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
