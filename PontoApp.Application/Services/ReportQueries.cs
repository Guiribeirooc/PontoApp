using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Domain.Interfaces;

namespace PontoApp.Application.Services
{
    public sealed class ReportQueries : IReportQueries
    {
        private readonly IPunchRepository _punchRepo;

        public ReportQueries(IPunchRepository punchRepo) => _punchRepo = punchRepo;

        public async Task<List<EspelhoDiaDto>> GetEspelhoAsync(
            DateOnly inicio, DateOnly fim, int? employeeId, CancellationToken ct = default)
        {
            var ini = inicio.ToDateTime(TimeOnly.MinValue);
            var fimEx = fim.AddDays(1).ToDateTime(TimeOnly.MinValue);

            var q = _punchRepo.Query()
                              .Where(p => p.DataHora >= ini && p.DataHora < fimEx);

            if (employeeId is > 0)
                q = q.Where(p => p.EmployeeId == employeeId.Value);

            var rows = await q.AsNoTracking()
                              .OrderBy(p => p.EmployeeId)
                              .ThenBy(p => p.DataHora)
                              .Select(p => new { p.DataHora, p.Tipo })
                              .ToListAsync(ct);

            var dias = rows
                .GroupBy(r => DateOnly.FromDateTime(r.DataHora.Date))
                .OrderBy(g => g.Key)
                .Select(g => new EspelhoDiaDto
                {
                    Dia = g.Key,
                    Marcas = g.Select(r => new EspelhoMarcacaoDto
                    {
                        Tipo = r.Tipo,
                        Hora = r.DataHora.TimeOfDay
                    }).ToList()
                })
                .ToList();

            return dias;
        }
    }
}
