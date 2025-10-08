namespace PontoApp.Domain.Entities
{
    public class OnCallPeriod
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string? Notes { get; set; }
    }
}