using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Domain.Entities;
using PontoApp.Infrastructure.EF;

namespace PontoApp.Application.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly AppDbContext _db;
        public ScheduleService(AppDbContext db) => _db = db;

        public Task<List<DayOff>> ListDayOffsAsync(int employeeId, DateOnly from, DateOnly to, CancellationToken ct)
            => _db.DayOffs.Where(x => x.EmployeeId == employeeId && x.Date >= from && x.Date <= to)
                          .OrderBy(x => x.Date).ToListAsync(ct);

        public async Task AddDayOffAsync(int employeeId, DateOnly date, string? reason, CancellationToken ct)
        {
            _db.DayOffs.Add(new DayOff { EmployeeId = employeeId, Date = date, Reason = reason });
            await _db.SaveChangesAsync(ct);
        }
    }
}
