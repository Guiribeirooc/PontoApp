namespace PontoApp.Domain.Entities
{
    public enum PunchType
    {
        Entrada = 1,
        SaidaAlmoco = 2,
        VoltaAlmoco = 3,
        Saida = 4
    }

    public class Punch
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime DataHora { get; set; }
        public PunchType Tipo { get; set; }
        public string? Ip { get; set; }
        public Employee? Employee { get; set; }
        public string? Justificativa { get; set; }
        public string? Origem { get; set; }
    }
}