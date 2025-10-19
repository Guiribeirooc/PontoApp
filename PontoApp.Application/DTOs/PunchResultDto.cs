using PontoApp.Domain.Enums;

namespace PontoApp.Application.DTOs
{
    public record PunchResultDto(
        long Id,
        int EmployeeId,
        string EmployeePin,
        string EmployeeNome,
        PunchType Tipo,
        DateTime DataHora
    );
}
