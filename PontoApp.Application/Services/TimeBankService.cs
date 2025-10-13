using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Domain.Entities;
using PontoApp.Infrastructure.EF;

namespace PontoApp.Application.Services
{
    public class TimeBankService(AppDbContext db) : ITimeBankService
    {
        private readonly AppDbContext _db = db;

        public async Task AddAdjustmentAsync(int employeeId, int minutes, string reason, CancellationToken ct)
        {
            _db.TimeBankEntries.Add(new TimeBankEntry
            {
                EmployeeId = employeeId,
                Minutes = minutes,
                Reason = reason,
                Source = "Manual",
                At = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }

        public async Task<int> GetBalanceMinutesAsync(int employeeId, DateOnly start, DateOnly end, CancellationToken ct)
        {
            var startDt = start.ToDateTime(TimeOnly.MinValue);
            var endDt = end.ToDateTime(TimeOnly.MaxValue);
            var ledger = await _db.TimeBankEntries
                .Where(x => x.EmployeeId == employeeId && x.At >= startDt && x.At <= endDt)
                .SumAsync(x => (int?)x.Minutes, ct) ?? 0;

            var punches = await _db.Punches.AsNoTracking()
                .Where(p => p.EmployeeId == employeeId && p.DataHora >= startDt && p.DataHora <= endDt)
                .OrderBy(p => p.DataHora)
                .ToListAsync(ct);

            var worked = 0;
            foreach (var grp in punches.GroupBy(p => p.DataHora.Date))
            {
                var list = grp.ToList();
                for (int i = 0; i + 1 < list.Count; i += 2)
                    worked += (int)(list[i + 1].DataHora - list[i].DataHora).TotalMinutes;
            }

            var days = Enumerable.Range(0, end.DayNumber - start.DayNumber + 1)
                                 .Select(n => start.AddDays(n))
                                 .Count(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday);

            var target = days * 8 * 60;
            return (worked - target) + ledger;
        }

        public Task<List<TimeBankEntry>> GetStatementAsync(int employeeId, DateOnly start, DateOnly end, CancellationToken ct)
        {
            var startDt = start.ToDateTime(TimeOnly.MinValue);
            var endDt = end.ToDateTime(TimeOnly.MaxValue);
            return _db.TimeBankEntries
                .Where(x => x.EmployeeId == employeeId && x.At >= startDt && x.At <= endDt)
                .OrderBy(x => x.At).ToListAsync(ct);
        }
    }
}
