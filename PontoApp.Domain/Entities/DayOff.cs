namespace PontoApp.Domain.Entities
{
    public class DayOff
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;
        public DateOnly Date { get; set; }
        public string? Reason { get; set; }
    }
}