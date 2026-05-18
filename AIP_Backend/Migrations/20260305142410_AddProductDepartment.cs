using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddProductDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: local/restored databases may already have Department from manual schema drift.
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Products', 'Department') IS NULL
                    ALTER TABLE dbo.Products ADD [Department] nvarchar(100) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Products', 'Department') IS NOT NULL
                    ALTER TABLE dbo.Products DROP COLUMN [Department];
                """);
        }
    }
}
