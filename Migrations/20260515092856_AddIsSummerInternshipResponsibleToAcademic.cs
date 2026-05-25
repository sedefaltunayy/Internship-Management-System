using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebTabanliStajTakipSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSummerInternshipResponsibleToAcademic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSummerInternshipResponsible",
                table: "Academics",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSummerInternshipResponsible",
                table: "Academics");
        }
    }
}
