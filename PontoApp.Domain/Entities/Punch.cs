using PontoApp.Domain.Enums;

namespace PontoApp.Domain.Entities
{
    public class Punch
    {
        public long Id { get; set; }
        public int CompanyId { get; set; }
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public DateTime DataHora { get; set; }        // datetime2
        public PunchType Tipo { get; set; }           // 1..4

        public string? Justificativa { get; set; }
        public string? Origem { get; set; }
        public string? SourceIp { get; set; }
        public string? Notes { get; set; }
    }
}