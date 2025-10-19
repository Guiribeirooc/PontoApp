namespace PontoApp.Domain.Entities;

public class OnCallPeriod
{
    public long Id { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
    public string? Notes { get; set; }
}
