using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Domain.Entities;
using PontoApp.Infrastructure.EF;

namespace PontoApp.Application.Services
{
    public class OnCallService : IOnCallService
    {
        private readonly AppDbContext _db;
        public OnCallService(AppDbContext db) => _db = db;

        public Task<List<OnCallPeriod>> ListAsync(int employeeId, DateTime from, DateTime to, CancellationToken ct)
            => _db.OnCallPeriods.Where(x => x.EmployeeId == employeeId && x.Start < to && x.End > from)
                                .OrderBy(x => x.Start).ToListAsync(ct);

        public async Task AddAsync(int employeeId, DateTime start, DateTime end, string? notes, CancellationToken ct)
        {
            _db.OnCallPeriods.Add(new OnCallPeriod { EmployeeId = employeeId, Start = start, End = end, Notes = notes });
            await _db.SaveChangesAsync(ct);
        }
    }
}
