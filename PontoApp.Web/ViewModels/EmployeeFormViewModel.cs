using Microsoft.AspNetCore.Mvc;
using PontoApp.Domain.Enums;
using PontoApp.Web.Binders;

namespace PontoApp.Web.ViewModels
{
    public class EmployeeFormViewModel
    {
        public int? Id { get; set; }
        public string Nome { get; set; } = "";
        public string Pin { get; set; } = "";
        [ModelBinder(BinderType = typeof(CpfModelBinder))]
        public string Cpf { get; set; } = "";
        public string Email { get; set; } = "";
        public DateOnly BirthDate { get; set; }
        public bool Ativo { get; set; } = true;
        public string? ShiftStart { get; set; }
        public string? ShiftEnd { get; set; }
        public IFormFile? Foto { get; set; }
        public string? FotoAtualPath { get; set; }
        public WorkScheduleKind Jornada { get; set; } = WorkScheduleKind.Integral;
        public string? Phone { get; set; }
        public string? NisPis { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Departamento { get; set; }
        public string? Cargo { get; set; }
        public string? Matricula { get; set; }
        public decimal? HourlyRate { get; set; }
        public DateOnly? AdmissionDate { get; set; }
        public bool HasTimeBank { get; set; }
        public DateOnly? TrackingStart { get; set; }
        public DateOnly? TrackingEnd { get; set; }
        public DateOnly? VacationAccrualStart { get; set; }
        public string? ManagerName { get; set; }
        public string? EmployerName { get; set; }
        public string? UnitName { get; set; }
    }
}
