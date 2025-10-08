using PontoApp.Domain.Enums;

namespace PontoApp.Domain.Entities
{
    public class Leave
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;
        public LeaveType Type { get; set; }
        public LeaveStatus Status { get; set; } = LeaveStatus.Approved;
        public DateOnly Start { get; set; }
        public DateOnly End { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
