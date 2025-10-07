namespace PontoApp.Application.DTOs;
public record WorkSummaryDto(
    DateOnly Inicio,
    DateOnly Fim,
    int? EmployeeId,
    string? EmployeeNome,
    List<WorkDayDto> Dias,
    TimeSpan TotalPeriodo
);

public record WorkDayDto(
    DateOnly Dia,
    int EmployeeId,
    string EmployeeNome,
    List<WorkIntervalDto> Intervalos,
    TimeSpan TotalDia,
    TimeSpan AjusteAlmoco,
    DateTimeOffset? SaidaAlmoco,
    DateTimeOffset? VoltaAlmoco
);

