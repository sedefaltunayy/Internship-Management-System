using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebTabanliStajTakipSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddFileStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcademicNote",
                table: "InternshipFiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSentToAcademic",
                table: "InternshipFiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentToAcademicDate",
                table: "InternshipFiles",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcademicNote",
                table: "InternshipFiles");

            migrationBuilder.DropColumn(
                name: "IsSentToAcademic",
                table: "InternshipFiles");

            migrationBuilder.DropColumn(
                name: "SentToAcademicDate",
                table: "InternshipFiles");
        }
    }
}
