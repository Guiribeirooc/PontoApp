namespace PontoApp.Domain.Interfaces;
public interface IPunchRepository {
    Task<Punch?> GetLastAsync(int employeeId, CancellationToken ct = default);
    Task AddAsync(Punch p, CancellationToken ct = default);
    IQueryable<Punch> Query();
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(int employeeId, PunchType tipo, DateTimeOffset dataHora, CancellationToken ct = default);
    Task<List<Punch>> ListByPeriodAsync(
    DateTimeOffset start, DateTimeOffset end, int? employeeId, CancellationToken ct = default);
}
