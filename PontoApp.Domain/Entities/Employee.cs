using System.ComponentModel.DataAnnotations;
using PontoApp.Domain.Enums;

namespace PontoApp.Domain.Entities
{
    public class Employee
    {
        public int Id { get; set; }
        [Required] public string Nome { get; set; } = "";
        [Required] public string Pin { get; set; } = "";
        [Required] public string Cpf { get; set; } = "";
        [Required] public string Email { get; set; } = "";
        [Required] public DateOnly BirthDate { get; set; }
        public string? PhotoPath { get; set; }
        public TimeOnly? ShiftStart { get; set; }
        public TimeOnly? ShiftEnd { get; set; }
        public bool Ativo { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public bool IsAdmin { get; set; }
        public ICollection<Punch> Punches { get; set; } = new List<Punch>();
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

