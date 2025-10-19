using PontoApp.Domain.Enums;

namespace PontoApp.Domain.Entities;

public class Leave
{
    public long Id { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public LeaveType Type { get; set; }
    public LeaveStatus Status { get; set; }
    public string? Notes { get; set; }

    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
}
