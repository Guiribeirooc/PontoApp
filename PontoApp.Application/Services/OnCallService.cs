using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Domain.Entities;
using PontoApp.Infrastructure.EF;

namespace PontoApp.Application.Services;

public class OnCallService(AppDbContext db) : IOnCallService
{
    private readonly AppDbContext _db = db;

    public async Task<List<OnCallPeriod>> ListAsync(int employeeId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        return await _db.OnCallPeriods
            .Where(x => x.EmployeeId == employeeId
                        && x.Start <= to
                        && x.End >= from)
            .OrderBy(x => x.Start)
            .ToListAsync(ct);
    }

    public async Task AddAsync(int employeeId, DateOnly start, DateOnly end, string? notes, CancellationToken ct)
    {
        _db.OnCallPeriods.Add(new OnCallPeriod
        {
            EmployeeId = employeeId,
            Start = start,
            End = end,
            Notes = notes
        });
        await _db.SaveChangesAsync(ct);
    }
}
