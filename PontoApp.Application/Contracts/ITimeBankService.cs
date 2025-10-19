using PontoApp.Application.DTOs;
using PontoApp.Domain.Entities;

namespace PontoApp.Application.Contracts
{
    public interface ITimeBankService
    {
        Task<int> GetBalanceMinutesAsync(int employeeId, DateOnly start, DateOnly end, CancellationToken ct);
        Task AddAdjustmentAsync(int employeeId, int minutes, string reason, CancellationToken ct);
        Task<IReadOnlyList<TimeBankStatementDto>> GetStatementAsync(int employeeId, DateOnly start, DateOnly end, CancellationToken ct);

    }
}
