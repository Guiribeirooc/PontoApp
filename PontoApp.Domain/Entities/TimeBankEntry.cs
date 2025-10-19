namespace PontoApp.Domain.Entities;

public class TimeBankEntry
{
    public long Id { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateOnly At { get; set; }
    public int Minutes { get; set; }           // positivo/negativo
    public string? Reason { get; set; }
    public string? Source { get; set; }
}
