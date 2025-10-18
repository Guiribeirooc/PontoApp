using PontoApp.Domain.Entities;

namespace PontoApp.Domain.Interfaces;
public interface IPunchRepository {
    Task<Punch?> GetLastAsync(int employeeId, CancellationToken ct = default);
    Task AddAsync(Punch p, CancellationToken ct = default);
    IQueryable<Punch> Query();
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(int employeeId, PunchType tipo, DateTime dataHora, CancellationToken ct = default);
    Task<List<Punch>> ListByPeriodAsync(
    DateTime start, DateTime end, int? employeeId, CancellationToken ct = default);
}
