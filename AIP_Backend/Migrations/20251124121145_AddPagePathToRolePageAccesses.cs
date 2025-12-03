using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddPagePathToRolePageAccesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PagePath",
                table: "RolePageAccesses",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE rpa
                SET PagePath = pa.Path
                FROM RolePageAccesses rpa
                INNER JOIN PageAccesses pa ON rpa.PageAccessId = pa.Id
                WHERE rpa.PagePath IS NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_RolePageAccesses_RoleName_PagePath",
                table: "RolePageAccesses",
                columns: new[] { "RoleName", "PagePath" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RolePageAccesses_RoleName_PagePath",
                table: "RolePageAccesses");

            migrationBuilder.DropColumn(
                name: "PagePath",
                table: "RolePageAccesses");
        }
    }
}
