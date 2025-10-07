namespace PontoApp.Application.DTOs;

public record PunchResultDto(
    int Id,
    int EmployeeId,
    string EmployeePin,
    string EmployeeNome,
    PunchType Tipo,
    DateTimeOffset DataHora
);