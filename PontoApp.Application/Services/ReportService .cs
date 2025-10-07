using Microsoft.EntityFrameworkCore;
using PontoApp.Application.DTOs;
using PontoApp.Domain.Interfaces;

namespace PontoApp.Application.Services;

public record WorkRules(int RoundingMinutes, int MinLunchMinutes, TimeSpan LunchWindowStart, TimeSpan LunchWindowEnd, double MaxDailyHours);

public interface IReportService
{
    Task<WorkSummaryDto> ResumoAsync(DateOnly inicio, DateOnly fim, int? employeeId = null, CancellationToken ct = default);
}

public class ReportService : IReportService
{
    private readonly IPunchRepository _punchRepo;
    private readonly IEmployeeRepository _empRepo;
    private readonly WorkRules _rules;

    public ReportService(IPunchRepository punchRepo, IEmployeeRepository empRepo, WorkRules rules)
    {
        _punchRepo = punchRepo; _empRepo = empRepo; _rules = rules;
    }

    private static TimeZoneInfo GetBrTz()
    {
        try
        {
            return OperatingSystem.IsWindows()
                ? TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")
                : TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch
        {
            return TimeZoneInfo.CreateCustomTimeZone("BR-Fallback", TimeSpan.FromHours(-3), "BR-Fallback", "BR-Fallback");
        }
    }

    private static (DateTimeOffset ini, DateTimeOffset fim) DiaRange(DateOnly dia, TimeZoneInfo tz)
    {
        var localMidnight = DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var ini = new DateTimeOffset(localMidnight, tz.GetUtcOffset(localMidnight));
        return (ini, ini.AddDays(1));
    }

    private TimeSpan Round(TimeSpan t)
    {
        var m = _rules.RoundingMinutes;
        if (m <= 1) return t;
        var rounded = Math.Round(t.TotalMinutes / m) * m;
        return TimeSpan.FromMinutes(rounded);
    }

    public async Task<WorkSummaryDto> ResumoAsync(DateOnly inicio, DateOnly fim, int? employeeId = null, CancellationToken ct = default)
    {
        if (fim < inicio) throw new InvalidOperationException("Período inválido.");

        var tz = GetBrTz();
        var (ini, _) = DiaRange(inicio, tz);
        var (_, fimExcl) = DiaRange(fim, tz);

        var punches = await (
            from p in _punchRepo.Query().AsNoTracking()
            join e in _empRepo.Query().AsNoTracking() on p.EmployeeId equals e.Id
            where p.DataHora >= ini && p.DataHora < fimExcl
                  && (employeeId == null || p.EmployeeId == employeeId.Value)
                  && e.Ativo && !e.IsDeleted
            select new
            {
                p.EmployeeId,
                EmployeeNome = e.Nome,
                p.DataHora,
                p.Tipo
            }
        )
        .OrderBy(x => x.EmployeeId)
        .ThenBy(x => x.DataHora)
        .ToListAsync(ct);

        var dias = new List<WorkDayDto>();
        var totalPeriodo = TimeSpan.Zero;

        foreach (var grpEmp in punches.GroupBy(x => new { x.EmployeeId, x.EmployeeNome }))
        {
            var porDia = grpEmp
                .GroupBy(x =>
                {
                    var local = x.DataHora.ToLocalTime();
                    var localDateOnly = DateOnly.FromDateTime(local.Date);
                    return localDateOnly;
                })
                .OrderBy(g => g.Key);

            foreach (var grpDia in porDia)
            {
                var pares = new List<WorkIntervalDto>();
                DateTimeOffset? aberto = null;

                foreach (var r in grpDia.OrderBy(p => p.DataHora))
                {
                    switch (r.Tipo)
                    {
                        case PunchType.Entrada:
                            aberto ??= r.DataHora;
                            break;

                        case PunchType.SaidaAlmoco:
                            if (aberto is not null)
                            {
                                pares.Add(new WorkIntervalDto(aberto.Value, null, TimeSpan.Zero));
                                aberto = null;
                            }
                            break;

                        case PunchType.VoltaAlmoco:
                            aberto ??= r.DataHora;
                            break;

                        case PunchType.Saida:
                            if (aberto is not null)
                            {
                                var dur = r.DataHora - aberto.Value;
                                if (dur < TimeSpan.Zero) dur = TimeSpan.Zero;
                                var durRounded = Round(dur);
                                pares.Add(new WorkIntervalDto(aberto.Value, r.DataHora, durRounded));
                                aberto = null;
                            }
                            break;
                    }
                }

                if (aberto is not null)
                    pares.Add(new WorkIntervalDto(aberto.Value, null, TimeSpan.Zero));

                var almocoMin = TimeSpan.FromMinutes(_rules.MinLunchMinutes);
                var lunchStartOnly = TimeOnly.FromTimeSpan(_rules.LunchWindowStart);
                var lunchEndOnly = TimeOnly.FromTimeSpan(_rules.LunchWindowEnd);
                var janelaIni = grpDia.Key.ToDateTime(lunchStartOnly);
                var janelaFim = grpDia.Key.ToDateTime(lunchEndOnly);

                TimeSpan maiorGapAlmoco = TimeSpan.Zero;
                DateTimeOffset? lastOut = null;

                foreach (var i in pares.Where(i => i.Out != null).OrderBy(i => i.In))
                {
                    if (lastOut is not null)
                    {
                        var interStart = lastOut.Value;
                        var interEnd = i.In;

                        var ei = interStart > janelaIni ? interStart : janelaIni;
                        var ef = interEnd < janelaFim ? interEnd : janelaFim;

                        var efetivo = (ef > ei) ? (ef - ei) : TimeSpan.Zero;
                        if (efetivo > maiorGapAlmoco) maiorGapAlmoco = efetivo;
                    }
                    lastOut = i.Out;
                }

                var ajusteAlmoco = TimeSpan.Zero;
                if (almocoMin > TimeSpan.Zero && maiorGapAlmoco < almocoMin)
                    ajusteAlmoco = almocoMin - maiorGapAlmoco;

                var totalDia = new TimeSpan(pares.Where(i => i.Out != null).Sum(i => i.Duration.Ticks));

                if (ajusteAlmoco > TimeSpan.Zero)
                {
                    totalDia -= ajusteAlmoco;
                    if (totalDia < TimeSpan.Zero) totalDia = TimeSpan.Zero;
                }

                var maxDia = TimeSpan.FromHours(_rules.MaxDailyHours);
                if (maxDia.TotalMinutes > 0 && totalDia > maxDia) totalDia = maxDia;

                totalDia = Round(totalDia);

                totalPeriodo += totalDia;

                var saidaAlmocoEvt = grpDia.FirstOrDefault(p => p.Tipo == PunchType.SaidaAlmoco)?.DataHora;
                var voltaAlmocoEvt = grpDia.FirstOrDefault(p => p.Tipo == PunchType.VoltaAlmoco)?.DataHora;

                dias.Add(new WorkDayDto(
                    grpDia.Key,
                    grpEmp.Key.EmployeeId,
                    grpEmp.Key.EmployeeNome,
                    pares,
                    totalDia,
                    ajusteAlmoco,
                    saidaAlmocoEvt,
                    voltaAlmocoEvt
                ));
            }
        }

        string? empNome = null;
        if (employeeId.HasValue && employeeId.Value > 0)
            empNome = (await _empRepo.GetByIdAsync(employeeId.Value, ct))?.Nome;

        return new WorkSummaryDto(
            inicio,
            fim,
            (employeeId.HasValue && employeeId.Value > 0) ? employeeId : null,
            empNome,
            dias.OrderBy(d => d.Dia).ThenBy(d => d.EmployeeId).ToList(),
            totalPeriodo
        );
    }
}
