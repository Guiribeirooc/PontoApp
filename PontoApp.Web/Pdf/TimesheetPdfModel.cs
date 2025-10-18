namespace PontoApp.Web.Pdf
{
    public sealed class TimesheetPdfModel
    {
        public string Funcionario { get; set; } = "";
        public string? Cpf { get; set; }
        public string? Pin { get; set; }
        public DateOnly Inicio { get; set; }
        public DateOnly Fim { get; set; }
        public List<TimesheetPdfDay> Dias { get; set; } = new();
        public TimeSpan BancoDeHoras { get; set; }
        public TimeSpan TotalPeriodo { get; set; }
        public TimeSpan TotalExtras { get; set; }
        public TimeSpan TotalAtrasos { get; set; }
        public string? Empresa { get; set; }
        public string? Cnpj { get; set; }
        public byte[]? LogoPng { get; set; }
    }

    public sealed class TimesheetPdfDay
    {
        public DateOnly Data { get; set; }
        public DateTime? Entrada { get; set; }
        public DateTime? SaidaAlmoco { get; set; }
        public DateTime? VoltaAlmoco { get; set; }
        public DateTime? Saida { get; set; }
        public TimeSpan? Total { get; set; }
        public TimeSpan? Extras { get; set; }
        public TimeSpan? Atraso { get; set; }     
        public string? Ocorrencia { get; set; } 
    }
}