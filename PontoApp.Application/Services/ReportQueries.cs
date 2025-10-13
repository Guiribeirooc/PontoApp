using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Domain.Interfaces;

namespace PontoApp.Application.Services;

public sealed class ReportQueries(IPunchRepository punchRepo) : IReportQueries
{
    private readonly IPunchRepository _punchRepo = punchRepo;

    private static TimeZoneInfo SaoPauloTz =>
        OperatingSystem.IsWindows()
        ? TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")
        : TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public async Task<List<EspelhoDiaDto>> GetEspelhoAsync(DateOnly inicio, DateOnly fim, int? employeeId, CancellationToken ct = default)
    {
        // [ini, fim+1) em SP
        var startLocal = inicio.ToDateTime(TimeOnly.MinValue);
        var endLocal = fim.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var startDto = new DateTimeOffset(startLocal, SaoPauloTz.GetUtcOffset(startLocal));
        var endDto = new DateTimeOffset(endLocal, SaoPauloTz.GetUtcOffset(endLocal));

        var q = _punchRepo.Query().Where(p => p.DataHora >= startDto && p.DataHora < endDto);
        if (employeeId is > 0) q = q.Where(p => p.EmployeeId == employeeId.Value);

        var rows = await q.AsNoTracking()
            .OrderBy(p => p.EmployeeId).ThenBy(p => p.DataHora)
            .Select(p => new { p.DataHora, p.Tipo })
            .ToListAsync(ct);

        var dias = rows
            .GroupBy(r =>
            {
                var off = SaoPauloTz.GetUtcOffset(r.DataHora.UtcDateTime);
                return DateOnly.FromDateTime(r.DataHora.ToOffset(off).Date);
            })
            .OrderBy(g => g.Key)
            .Select(g => new EspelhoDiaDto
            {
                Dia = g.Key,
                Marcas = g.Select(r =>
                {
                    var off = SaoPauloTz.GetUtcOffset(r.DataHora.UtcDateTime);
                    var loc = r.DataHora.ToOffset(off);
                    return new EspelhoMarcacaoDto { Tipo = r.Tipo, Hora = loc.TimeOfDay };
                }).ToList()
            })
            .ToList();

        return dias;
    }
}
