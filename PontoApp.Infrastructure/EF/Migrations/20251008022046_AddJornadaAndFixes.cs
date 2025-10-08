using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PontoApp.Infrastructure.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddJornadaAndFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Jornada",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Jornada",
                table: "Employees");
        }
    }
}
