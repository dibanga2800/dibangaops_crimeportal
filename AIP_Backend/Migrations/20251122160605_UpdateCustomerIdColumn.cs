using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustomerIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrate data from UserCompany (string) to CustomerId (int)
            // This matches UserCompany values to Customer.CompanyName and sets the CustomerId
            migrationBuilder.Sql(@"
                UPDATE u
                SET u.CustomerId = c.CustomerId
                FROM AspNetUsers u
                INNER JOIN Customers c ON u.UserCompany = c.CompanyName
                WHERE u.UserCompany IS NOT NULL 
                    AND u.UserCompany != ''
                    AND u.CustomerId IS NULL
            ");

            // Drop the UserCompany column
            migrationBuilder.DropColumn(
                name: "UserCompany",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add the UserCompany column
            migrationBuilder.AddColumn<string>(
                name: "UserCompany",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Migrate data back from CustomerId to UserCompany (if needed for rollback)
            migrationBuilder.Sql(@"
                UPDATE u
                SET u.UserCompany = c.CompanyName
                FROM AspNetUsers u
                INNER JOIN Customers c ON u.CustomerId = c.CustomerId
                WHERE u.CustomerId IS NOT NULL
            ");
        }
    }
}
