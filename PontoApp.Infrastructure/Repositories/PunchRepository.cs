using Microsoft.EntityFrameworkCore;
using PontoApp.Domain.Entities;
using PontoApp.Domain.Enums;
using PontoApp.Domain.Interfaces;
using PontoApp.Infrastructure.EF;

namespace PontoApp.Infrastructure.Repositories;

public class PunchRepository : IPunchRepository
{
    private readonly AppDbContext _db;
    public PunchRepository(AppDbContext db) => _db = db;

    public Task<Punch?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Punches.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Punch?> GetLastAsync(int employeeId, CancellationToken ct = default) =>
        _db.Punches
           .Where(p => p.EmployeeId == employeeId)
           .OrderByDescending(p => p.DataHora)
           .FirstOrDefaultAsync(ct);

    public Task AddAsync(Punch p, CancellationToken ct = default)
    {
        _db.Punches.Add(p);
        return Task.CompletedTask;
    }

    public IQueryable<Punch> Query() =>
        _db.Punches.AsNoTracking();

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);

    public Task<bool> ExistsAsync(int employeeId, PunchType tipo, DateTime dataHora, CancellationToken ct = default) =>
        _db.Punches
           .AsNoTracking()
           .AnyAsync(p =>
               p.EmployeeId == employeeId &&
               p.Tipo == tipo &&
               p.DataHora == dataHora, ct);

    public async Task<List<Punch>> ListByPeriodAsync(
        DateTime start, DateTime end, int? employeeId, CancellationToken ct = default)
    {
        var q = _db.Punches
                   .AsNoTracking()
                   .Include(p => p.Employee)
                   .Where(p => p.DataHora >= start && p.DataHora < end);

        if (employeeId.HasValue)
            q = q.Where(p => p.EmployeeId == employeeId.Value);

        return await q.OrderBy(p => p.Employee.Nome)
                      .ThenBy(p => p.DataHora)
                      .ToListAsync(ct);
    }
}
