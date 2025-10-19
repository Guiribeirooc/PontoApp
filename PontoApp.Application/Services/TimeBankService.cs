using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Domain.Entities;
using PontoApp.Domain.Interfaces;
using PontoApp.Infrastructure.EF;

namespace PontoApp.Application.Services
{
    public class TimeBankService : ITimeBankService
    {
        private readonly AppDbContext _db;
        public TimeBankService(AppDbContext db) => _db = db;

        public async Task AddAdjustmentAsync(int employeeId, int minutes, string reason, CancellationToken ct)
        {
            _db.TimeBankEntries.Add(new TimeBankEntry
            {
                EmployeeId = employeeId,
                Minutes = minutes,
                Reason = reason,
                Source = "Manual",
                At = DateOnly.FromDateTime(DateTime.UtcNow) // ✅ corrigido
            });

            await _db.SaveChangesAsync(ct);
        }

        public async Task<int> GetBalanceMinutesAsync(int employeeId, DateOnly start, DateOnly end, CancellationToken ct)
        {
            // Soma dos ajustes manuais no período
            var ledger = await _db.TimeBankEntries
                .Where(x => x.EmployeeId == employeeId && x.At >= start && x.At <= end)
                .SumAsync(x => (int?)x.Minutes, ct) ?? 0;

            // Marcações do funcionário no período
            var punches = await _db.Punches
                .AsNoTracking()
                .Where(p => p.EmployeeId == employeeId &&
                            DateOnly.FromDateTime(p.DataHora) >= start &&
                            DateOnly.FromDateTime(p.DataHora) <= end)
                .OrderBy(p => p.DataHora)
                .ToListAsync(ct);

            var worked = 0;

            foreach (var grp in punches.GroupBy(p => p.DataHora.Date))
            {
                var dayPunches = grp.OrderBy(p => p.DataHora).ToList();
                var minutesDay = 0;

                // Pega pares de marcações (Entrada → Saída, SaídaAlmoço → VoltaAlmoço, etc.)
                for (int i = 0; i < dayPunches.Count - 1; i += 2)
                {
                    var inTime = dayPunches[i].DataHora;
                    var outTime = dayPunches[i + 1].DataHora;
                    var diff = (outTime - inTime).TotalMinutes;

                    if (diff > 0 && diff < 720) // ignora valores absurdos (>12h)
                        minutesDay += (int)diff;
                }

                worked += minutesDay;
            }

            // Retorna total de minutos (ajustes + trabalho efetivo)
            return ledger + worked;
        }

        public async Task<IReadOnlyList<TimeBankStatementDto>> GetStatementAsync(int employeeId, DateOnly start, DateOnly end, CancellationToken ct)
        {
            var query = _db.TimeBankEntries
                .AsNoTracking()
                .Where(x => x.EmployeeId == employeeId && x.At >= start && x.At <= end)
                .OrderBy(x => x.At)
                .Select(x => new TimeBankStatementDto(
                    x.At,
                    x.Minutes,
                    x.Reason,
                    x.Source
                ));

            return await query.ToListAsync(ct);
        }
    }
}
