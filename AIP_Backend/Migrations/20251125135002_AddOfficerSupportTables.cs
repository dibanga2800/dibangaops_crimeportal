using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficerSupportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OfficerSupportUpdates",
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
                    table.PrimaryKey("PK_OfficerSupportUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfficerSupportUpdates_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficerSupportUpdates_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfficerSupportUpdates_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficerSupportUpdates_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OfficerSupportDeclarations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UpdateId = table.Column<int>(type: "int", nullable: false),
                    OfficerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OfficerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
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
                    table.PrimaryKey("PK_OfficerSupportDeclarations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfficerSupportDeclarations_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficerSupportDeclarations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfficerSupportDeclarations_AspNetUsers_OfficerUserId",
                        column: x => x.OfficerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficerSupportDeclarations_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficerSupportDeclarations_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfficerSupportDeclarations_OfficerSupportUpdates_UpdateId",
                        column: x => x.UpdateId,
                        principalTable: "OfficerSupportUpdates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportDeclarations_Acknowledged",
                table: "OfficerSupportDeclarations",
                column: "Acknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportDeclarations_CreatedBy",
                table: "OfficerSupportDeclarations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportDeclarations_CreatedByUserId",
                table: "OfficerSupportDeclarations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportDeclarations_OfficerUserId",
                table: "OfficerSupportDeclarations",
                column: "OfficerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportDeclarations_SignatureDate",
                table: "OfficerSupportDeclarations",
                column: "SignatureDate");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportDeclarations_UpdatedBy",
                table: "OfficerSupportDeclarations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportDeclarations_UpdatedByUserId",
                table: "OfficerSupportDeclarations",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportDeclarations_UpdateId",
                table: "OfficerSupportDeclarations",
                column: "UpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportUpdates_CreatedAt",
                table: "OfficerSupportUpdates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportUpdates_CreatedBy",
                table: "OfficerSupportUpdates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportUpdates_CreatedByUserId",
                table: "OfficerSupportUpdates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportUpdates_EffectiveDate",
                table: "OfficerSupportUpdates",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportUpdates_Status",
                table: "OfficerSupportUpdates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportUpdates_UpdatedBy",
                table: "OfficerSupportUpdates",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerSupportUpdates_UpdatedByUserId",
                table: "OfficerSupportUpdates",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfficerSupportDeclarations");

            migrationBuilder.DropTable(
                name: "OfficerSupportUpdates");
        }
    }
}
