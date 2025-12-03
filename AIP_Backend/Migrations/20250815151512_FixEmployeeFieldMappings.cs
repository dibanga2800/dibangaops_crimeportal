using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class FixEmployeeFieldMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_AspNetUsers_UpdatedBy",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "Employees",
                newName: "ModifiedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Employees",
                newName: "DateModified");

            migrationBuilder.RenameColumn(
                name: "UniformIssued",
                table: "Employees",
                newName: "UniformIssuedYN");

            migrationBuilder.RenameColumn(
                name: "QuickStarterFormCompleted",
                table: "Employees",
                newName: "QuickStarterFormCompletedYN");

            migrationBuilder.RenameColumn(
                name: "PhotoTaken",
                table: "Employees",
                newName: "PhotoTakenYN");

            migrationBuilder.RenameColumn(
                name: "IdCardIssued",
                table: "Employees",
                newName: "IDCardIssuedYN");

            migrationBuilder.RenameColumn(
                name: "EquipmentIssued",
                table: "Employees",
                newName: "EquipmentIssuedYN");

            migrationBuilder.RenameColumn(
                name: "DrivingLicenceCopyTaken",
                table: "Employees",
                newName: "DrivingLicenceCopyTakenYN");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Employees",
                newName: "DateCreated");

            migrationBuilder.RenameColumn(
                name: "ContractOfEmploymentSigned",
                table: "Employees",
                newName: "ContractOfEmploymentSignedYN");

            migrationBuilder.RenameIndex(
                name: "IX_Employees_UpdatedBy",
                table: "Employees",
                newName: "IX_Employees_ModifiedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_AspNetUsers_ModifiedBy",
                table: "Employees",
                column: "ModifiedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_AspNetUsers_ModifiedBy",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "UniformIssuedYN",
                table: "Employees",
                newName: "UniformIssued");

            migrationBuilder.RenameColumn(
                name: "QuickStarterFormCompletedYN",
                table: "Employees",
                newName: "QuickStarterFormCompleted");

            migrationBuilder.RenameColumn(
                name: "PhotoTakenYN",
                table: "Employees",
                newName: "PhotoTaken");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "Employees",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "IDCardIssuedYN",
                table: "Employees",
                newName: "IdCardIssued");

            migrationBuilder.RenameColumn(
                name: "EquipmentIssuedYN",
                table: "Employees",
                newName: "EquipmentIssued");

            migrationBuilder.RenameColumn(
                name: "DrivingLicenceCopyTakenYN",
                table: "Employees",
                newName: "DrivingLicenceCopyTaken");

            migrationBuilder.RenameColumn(
                name: "DateModified",
                table: "Employees",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "DateCreated",
                table: "Employees",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ContractOfEmploymentSignedYN",
                table: "Employees",
                newName: "ContractOfEmploymentSigned");

            migrationBuilder.RenameIndex(
                name: "IX_Employees_ModifiedBy",
                table: "Employees",
                newName: "IX_Employees_UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_AspNetUsers_UpdatedBy",
                table: "Employees",
                column: "UpdatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
