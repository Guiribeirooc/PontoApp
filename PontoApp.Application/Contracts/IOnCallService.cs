using PontoApp.Domain.Entities;

namespace PontoApp.Application.Contracts
{
    public interface IOnCallService
    {
        Task<List<OnCallPeriod>> ListAsync(int employeeId, DateTime from, DateTime to, CancellationToken ct);
        Task AddAsync(int employeeId, DateTime start, DateTime end, string? notes, CancellationToken ct);
    }
}
