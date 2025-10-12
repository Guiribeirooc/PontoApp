using PontoApp.Application.DTOs;

namespace PontoApp.Application.Contracts
{
    public interface IReportQueries
    {
        Task<List<EspelhoDiaDto>> GetEspelhoAsync(DateOnly inicio, DateOnly fim, int? employeeId, CancellationToken ct = default);
    }
}