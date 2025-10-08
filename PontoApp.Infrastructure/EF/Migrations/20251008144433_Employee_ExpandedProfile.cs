using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PontoApp.Infrastructure.EF.Migrations
{
    /// <inheritdoc />
    public partial class Employee_ExpandedProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "AdmissionDate",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cargo",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployerName",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasTimeBank",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "Employees",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerName",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Matricula",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NisPis",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TrackingEnd",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TrackingStart",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitName",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "VacationAccrualStart",
                table: "Employees",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdmissionDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Cargo",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmployerName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "HasTimeBank",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ManagerName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Matricula",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "NisPis",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TrackingEnd",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TrackingStart",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "UnitName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "VacationAccrualStart",
                table: "Employees");
        }
    }
}
