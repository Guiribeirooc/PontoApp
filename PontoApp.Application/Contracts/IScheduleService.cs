using PontoApp.Domain.Entities;

namespace PontoApp.Application.Contracts
{
    public interface IScheduleService
    {
        Task<List<DayOff>> ListDayOffsAsync(int employeeId, DateOnly from, DateOnly to, CancellationToken ct);
        Task AddDayOffAsync(int employeeId, DateOnly date, string? reason, CancellationToken ct);
    }
}
