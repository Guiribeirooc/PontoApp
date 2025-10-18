namespace PontoApp.Application.DTOs
{
    public class WorkDayDto
    {
        public DateOnly Dia { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeNome { get; set; } = string.Empty;
        public List<WorkIntervalDto> Intervalos { get; set; } = new();
        public TimeSpan TotalDia { get; set; }
        public TimeSpan AjusteAlmoco { get; set; }
        public DateTime? SaidaAlmoco { get; set; }
        public DateTime? VoltaAlmoco { get; set; }
        public TimeSpan HorasExtras { get; set; }
        public TimeSpan MinutosAtraso { get; set; }
        public TimeSpan BancoDeHoras { get; set; }
        public List<string> Ocorrencias { get; set; } = new();
    }


    public record WorkSummaryDto(
        DateOnly Inicio,
        DateOnly Fim,
        int? EmployeeId,
        string? EmployeeNome,
        List<WorkDayDto> Dias,
        TimeSpan TotalPeriodo,
        TimeSpan BancoDeHoras
    );
}