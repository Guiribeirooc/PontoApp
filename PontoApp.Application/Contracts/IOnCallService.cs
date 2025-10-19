using PontoApp.Domain.Entities;

namespace PontoApp.Application.Contracts
{
    public interface IOnCallService
    {
        Task<List<OnCallPeriod>> ListAsync(int employeeId, DateOnly from, DateOnly to, CancellationToken ct);
        Task AddAsync(int employeeId, DateOnly start, DateOnly end, string? notes, CancellationToken ct);
    }
}
