using System.ComponentModel.DataAnnotations;

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
        public DateTimeOffset? DeletedAt { get; set; }
        public bool IsAdmin { get; set; }
        public ICollection<Punch> Punches { get; set; } = new List<Punch>();
    }
}

