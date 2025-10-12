using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Domain.Interfaces;

namespace PontoApp.Application.Services
{
    public sealed class ReportQueries(IPunchRepository punchRepo) : IReportQueries
    {
        private readonly IPunchRepository _punchRepo = punchRepo;
        private static readonly TimeSpan Tz = TimeSpan.FromHours(-3);

        public async Task<List<EspelhoDiaDto>> GetEspelhoAsync(DateOnly inicio, DateOnly fim, int? employeeId, CancellationToken ct = default)
        {
            var iniDt = new DateTimeOffset(inicio.ToDateTime(TimeOnly.MinValue), Tz);
            var fimDt = new DateTimeOffset(fim.ToDateTime(TimeOnly.MinValue), Tz).AddDays(1);

            var q = _punchRepo.Query().Where(p => p.DataHora >= iniDt && p.DataHora < fimDt);

            if (employeeId.HasValue && employeeId.Value > 0)
                q = q.Where(p => p.EmployeeId == employeeId.Value);

            var rows = await q
                .Select(p => new
                {
                    Dia = DateOnly.FromDateTime(p.DataHora.ToOffset(Tz).Date),
                    Hora = p.DataHora.ToOffset(Tz).TimeOfDay,
                    p.Tipo
                })
                .OrderBy(x => x.Dia).ThenBy(x => x.Hora)
                .ToListAsync(ct);

            var dias = rows
                .GroupBy(x => x.Dia)
                .OrderBy(g => g.Key)
                .Select(g => new EspelhoDiaDto
                {
                    Dia = g.Key,
                    Marcas = g.Select(x => new EspelhoMarcacaoDto
                    {
                        Tipo = x.Tipo,
                        Hora = x.Hora
                    }).ToList()
                })
                .ToList();

            return dias;
        }
    }
}