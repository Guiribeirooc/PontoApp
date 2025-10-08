namespace PontoApp.Domain.Entities
{
    public class TimeBankEntry
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;
        public DateTime At { get; set; } = DateTime.UtcNow;
        public int Minutes { get; set; }
        public string Reason { get; set; } = "";
        public string Source { get; set; } = "Manual";
    }
}
