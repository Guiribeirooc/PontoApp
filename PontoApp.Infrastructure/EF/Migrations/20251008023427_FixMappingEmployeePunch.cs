using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PontoApp.Infrastructure.EF.Migrations
{
    /// <inheritdoc />
    public partial class FixMappingEmployeePunch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Punches_Employees_EmployeeId1",
                table: "Punches");

            migrationBuilder.DropIndex(
                name: "IX_Punches_EmployeeId1",
                table: "Punches");

            migrationBuilder.DropColumn(
                name: "EmployeeId1",
                table: "Punches");

            migrationBuilder.AlterColumn<string>(
                name: "Cpf",
                table: "Employees",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(14)",
                oldMaxLength: 14);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeId1",
                table: "Punches",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Cpf",
                table: "Employees",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(11)",
                oldMaxLength: 11);

            migrationBuilder.CreateIndex(
                name: "IX_Punches_EmployeeId1",
                table: "Punches",
                column: "EmployeeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Punches_Employees_EmployeeId1",
                table: "Punches",
                column: "EmployeeId1",
                principalTable: "Employees",
                principalColumn: "Id");
        }
    }
}
