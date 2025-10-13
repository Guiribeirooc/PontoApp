namespace PontoApp.Application.DTOs
{
    public class TimesheetPdfDto
    {
        public DateOnly Inicio { get; set; }
        public DateOnly Fim { get; set; }
        public string Funcionario { get; set; } = "";
        public List<TimesheetPdfDayDto> Dias { get; set; } = new();
        public TimeSpan BancoDeHoras { get; set; }
    }

    public class TimesheetPdfDayDto
    {
        public DateOnly Data { get; set; }
        public DateTimeOffset? Entrada { get; set; }
        public DateTimeOffset? Saida { get; set; }
        public DateTimeOffset? SaidaAlmoco { get; set; }
        public DateTimeOffset? VoltaAlmoco { get; set; }
        public TimeSpan? Total { get; set; }
        public TimeSpan? Extras { get; set; }
        public TimeSpan? Atraso { get; set; }
    }
}
