using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerSupportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagerSupportUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerSupportUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManagerSupportUpdates_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerSupportUpdates_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManagerSupportUpdates_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerSupportUpdates_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ManagerSupportDeclarations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UpdateId = table.Column<int>(type: "int", nullable: false),
                    ManagerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ManagerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SignatureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Signature = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Acknowledged = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerSupportDeclarations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManagerSupportDeclarations_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerSupportDeclarations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManagerSupportDeclarations_AspNetUsers_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerSupportDeclarations_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerSupportDeclarations_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManagerSupportDeclarations_ManagerSupportUpdates_UpdateId",
                        column: x => x.UpdateId,
                        principalTable: "ManagerSupportUpdates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportDeclarations_Acknowledged",
                table: "ManagerSupportDeclarations",
                column: "Acknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportDeclarations_CreatedBy",
                table: "ManagerSupportDeclarations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportDeclarations_CreatedByUserId",
                table: "ManagerSupportDeclarations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportDeclarations_ManagerUserId",
                table: "ManagerSupportDeclarations",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportDeclarations_SignatureDate",
                table: "ManagerSupportDeclarations",
                column: "SignatureDate");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportDeclarations_UpdatedBy",
                table: "ManagerSupportDeclarations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportDeclarations_UpdatedByUserId",
                table: "ManagerSupportDeclarations",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportDeclarations_UpdateId",
                table: "ManagerSupportDeclarations",
                column: "UpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportUpdates_CreatedAt",
                table: "ManagerSupportUpdates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportUpdates_CreatedBy",
                table: "ManagerSupportUpdates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportUpdates_CreatedByUserId",
                table: "ManagerSupportUpdates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportUpdates_EffectiveDate",
                table: "ManagerSupportUpdates",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportUpdates_Status",
                table: "ManagerSupportUpdates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportUpdates_UpdatedBy",
                table: "ManagerSupportUpdates",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerSupportUpdates_UpdatedByUserId",
                table: "ManagerSupportUpdates",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagerSupportDeclarations");

            migrationBuilder.DropTable(
                name: "ManagerSupportUpdates");
        }
    }
}
