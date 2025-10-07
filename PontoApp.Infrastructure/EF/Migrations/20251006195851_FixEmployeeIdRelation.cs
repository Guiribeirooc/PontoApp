using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PontoApp.Infrastructure.EF.Migrations
{
    /// <inheritdoc />
    public partial class FixEmployeeIdRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeId1",
                table: "Punches",
                type: "int",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
