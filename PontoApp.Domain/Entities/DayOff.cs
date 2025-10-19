namespace PontoApp.Domain.Entities;

public class DayOff
{
    public long Id { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateOnly Date { get; set; }
    public string? Reason { get; set; }
}
