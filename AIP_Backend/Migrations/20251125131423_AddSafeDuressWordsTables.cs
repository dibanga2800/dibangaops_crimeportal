using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSafeDuressWordsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodeWordHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OldWord = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NewWord = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AuthorizedCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeWordHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeWordHistories_AspNetUsers_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CodeWords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Word = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeWords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeWords_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodeWords_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeWordHistories_ChangedAt",
                table: "CodeWordHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CodeWordHistories_ChangedBy",
                table: "CodeWordHistories",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CodeWordHistories_Type",
                table: "CodeWordHistories",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CodeWordHistories_Type_ChangedAt",
                table: "CodeWordHistories",
                columns: new[] { "Type", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CodeWords_CreatedBy",
                table: "CodeWords",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CodeWords_Type",
                table: "CodeWords",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CodeWords_Type_UpdatedAt",
                table: "CodeWords",
                columns: new[] { "Type", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CodeWords_UpdatedBy",
                table: "CodeWords",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeWordHistories");

            migrationBuilder.DropTable(
                name: "CodeWords");
        }
    }
}
