using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebTabanliStajTakipSistemi.Migrations
{
    /// <inheritdoc />
    public partial class InternshipWorkflowUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcademicNote",
                table: "Internships",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApplicationApprovedDate",
                table: "Internships",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractApprovedDate",
                table: "Internships",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractUploadedDate",
                table: "Internships",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApplicationApproved",
                table: "Internships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsContractApproved",
                table: "Internships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "FileType",
                table: "InternshipFiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcademicNote",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "ApplicationApprovedDate",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "ContractApprovedDate",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "ContractUploadedDate",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "IsApplicationApproved",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "IsContractApproved",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "InternshipFiles");
        }
    }
}
