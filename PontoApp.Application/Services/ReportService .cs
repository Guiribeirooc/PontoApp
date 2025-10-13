using Microsoft.EntityFrameworkCore;
using PontoApp.Application.DTOs;
using PontoApp.Domain.Enums;
using PontoApp.Domain.Interfaces;

namespace PontoApp.Application.Services;

public record WorkRules(
    int RoundingMinutes,
    int MinLunchMinutes,
    TimeSpan LunchWindowStart,
    TimeSpan LunchWindowEnd,
    double MaxDailyHours
);

public interface IReportService
{
    Task<WorkSummaryDto> ResumoAsync(DateOnly inicio, DateOnly fim, int? employeeId = null, CancellationToken ct = default);
}

public class ReportService(IPunchRepository punchRepo, IEmployeeRepository empRepo, WorkRules rules) : IReportService
{
    private readonly IPunchRepository _punchRepo = punchRepo;
    private readonly IEmployeeRepository _empRepo = empRepo;
    private readonly WorkRules _rules = rules;

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
        var minutes = Math.Round(t.TotalMinutes / m, MidpointRounding.AwayFromZero) * m;
        return TimeSpan.FromMinutes(minutes);
    }

    private static TimeSpan Previsto(WorkScheduleKind jornada, bool workedToday)
    {
        if (!workedToday)
            return TimeSpan.Zero;

        // ajuste conforme sua regra
        return jornada switch
        {
            WorkScheduleKind.Integral => TimeSpan.FromHours(8),
            WorkScheduleKind.Parcial => TimeSpan.FromHours(4),
            WorkScheduleKind.DozePorTrintaSeis => TimeSpan.FromHours(12),
            WorkScheduleKind.Noturna => TimeSpan.FromHours(7),
            WorkScheduleKind.Remota => TimeSpan.FromHours(8),
            WorkScheduleKind.Intermitente => TimeSpan.Zero,
            WorkScheduleKind.Estagiario => TimeSpan.FromHours(6),

            _ => TimeSpan.Zero
        };
    }

    private (TimeSpan atraso, List<string> ocorr)
        CalcAtraso(DateTimeOffset? entrada, DateTimeOffset? saida, TimeOnly? sStart, TimeOnly? sEnd)
    {
        var ocorr = new List<string>();
        var atraso = TimeSpan.Zero;

        if (sStart.HasValue && entrada.HasValue)
        {
            var esperado = entrada.Value.Date + sStart.Value.ToTimeSpan();
            var diff = entrada.Value - esperado;
            if (diff > TimeSpan.FromMinutes(5)) atraso += diff; // tolerância simples
        }

        if (sEnd.HasValue && saida.HasValue)
        {
            var esperado = saida.Value.Date + sEnd.Value.ToTimeSpan();
            var diff = esperado - saida.Value; // saída antecipada
            if (diff > TimeSpan.Zero) atraso += diff;
        }

        return (Round(atraso), ocorr);
    }

    public async Task<WorkSummaryDto> ResumoAsync(
        DateOnly inicio, DateOnly fim, int? employeeId = null, CancellationToken ct = default)
    {
        if (fim < inicio) throw new InvalidOperationException("Período inválido.");

        var tz = GetBrTz();

        // [ini, fim+1) no fuso de SP
        var (ini, _) = DiaRange(inicio, tz);
        var (_, fimEx) = DiaRange(fim, tz);

        var rows = await (
            from p in _punchRepo.Query().AsNoTracking()
            join e in _empRepo.Query().AsNoTracking() on p.EmployeeId equals e.Id
            where p.DataHora >= ini && p.DataHora < fimEx
                  && (employeeId == null || p.EmployeeId == employeeId.Value)
                  && e.Ativo && !e.IsDeleted
            select new
            {
                p.EmployeeId,
                e.Nome,
                e.Jornada,
                e.ShiftStart,
                e.ShiftEnd,
                p.DataHora,
                p.Tipo
            }
        )
        .OrderBy(x => x.EmployeeId)
        .ThenBy(x => x.DataHora)
        .ToListAsync(ct);

        var dias = new List<WorkDayDto>();
        TimeSpan totalPeriodo = TimeSpan.Zero;
        TimeSpan bancoDoPeriodo = TimeSpan.Zero;

        foreach (var gEmp in rows.GroupBy(x => new { x.EmployeeId, x.Nome, x.Jornada, x.ShiftStart, x.ShiftEnd }))
        {
            // Agrupa por dia já normalizado para SP
            var porDia = gEmp
                .GroupBy(x =>
                {
                    var local = ToSp(x.DataHora, tz);
                    return DateOnly.FromDateTime(local.Date);
                })
                .OrderBy(g => g.Key);

            foreach (var gDia in porDia)
            {
                var pares = new List<WorkIntervalDto>();
                DateTimeOffset? aberto = null;

                foreach (var r in gDia.OrderBy(p => p.DataHora))
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
                                pares.Add(new WorkIntervalDto(aberto.Value, r.DataHora, Round(dur)));
                                aberto = null;
                            }
                            break;
                    }
                }
                if (aberto is not null)
                    pares.Add(new WorkIntervalDto(aberto.Value, null, TimeSpan.Zero));

                // ===== Janela de almoço no MESMO OFFSET do dia =====
                var dayOffset = tz.GetUtcOffset(gDia.Key.ToDateTime(TimeOnly.MinValue));
                var janelaIni = new DateTimeOffset(
                    gDia.Key.ToDateTime(TimeOnly.FromTimeSpan(_rules.LunchWindowStart)), dayOffset);
                var janelaFim = new DateTimeOffset(
                    gDia.Key.ToDateTime(TimeOnly.FromTimeSpan(_rules.LunchWindowEnd)), dayOffset);

                var almMin = TimeSpan.FromMinutes(_rules.MinLunchMinutes);

                TimeSpan maiorGapAlmoco = TimeSpan.Zero;
                DateTimeOffset? lastOut = null;

                foreach (var i in pares.Where(i => i.Out != null).OrderBy(i => i.In))
                {
                    if (lastOut is not null)
                    {
                        // Compare tudo no offset do dia
                        var interStart = lastOut.Value.ToOffset(dayOffset);
                        var interEnd = i.In.ToOffset(dayOffset);

                        var ei = interStart > janelaIni ? interStart : janelaIni;
                        var ef = interEnd < janelaFim ? interEnd : janelaFim;

                        var efetivo = (ef > ei) ? (ef - ei) : TimeSpan.Zero;
                        if (efetivo > maiorGapAlmoco) maiorGapAlmoco = efetivo;
                    }
                    lastOut = i.Out;
                }

                var ajusteAlmoco = (almMin > TimeSpan.Zero && maiorGapAlmoco < almMin)
                    ? (almMin - maiorGapAlmoco)
                    : TimeSpan.Zero;

                // Total trabalhado
                var trabalhado = new TimeSpan(pares.Where(i => i.Out != null).Sum(i => i.Duration.Ticks));
                if (ajusteAlmoco > TimeSpan.Zero)
                {
                    trabalhado -= ajusteAlmoco;
                    if (trabalhado < TimeSpan.Zero) trabalhado = TimeSpan.Zero;
                }

                var maxDia = TimeSpan.FromHours(_rules.MaxDailyHours);
                if (maxDia > TimeSpan.Zero && trabalhado > maxDia) trabalhado = maxDia;

                trabalhado = Round(trabalhado);

                var entrada = pares.FirstOrDefault()?.In;
                var saida = pares.LastOrDefault(i => i.Out != null)?.Out;

                // Atraso do dia
                var (atraso, ocorrAtraso) = CalcAtraso(entrada, saida, gEmp.Key.ShiftStart, gEmp.Key.ShiftEnd);

                // Previsto x banco/extras
                var previsto = Previsto(gEmp.Key.Jornada, workedToday: trabalhado > TimeSpan.Zero);
                var bancoDia = trabalhado - previsto;
                var extras = bancoDia > TimeSpan.Zero ? bancoDia : TimeSpan.Zero;

                var ocorr = new List<string>();
                if (ajusteAlmoco > TimeSpan.Zero)
                    ocorr.Add($"Almoço abaixo de {_rules.MinLunchMinutes} min (ajuste {Round(ajusteAlmoco):hh\\:mm}).");
                ocorr.AddRange(ocorrAtraso);
                if (entrada == null || saida == null) ocorr.Add("Marcações incompletas.");

                totalPeriodo += trabalhado;
                bancoDoPeriodo += bancoDia;

                var saidaAlmocoEvt = gDia.FirstOrDefault(p => p.Tipo == PunchType.SaidaAlmoco)?.DataHora;
                var voltaAlmocoEvt = gDia.FirstOrDefault(p => p.Tipo == PunchType.VoltaAlmoco)?.DataHora;

                dias.Add(new WorkDayDto
                {
                    Dia = gDia.Key,
                    EmployeeId = gEmp.Key.EmployeeId,
                    EmployeeNome = gEmp.Key.Nome,
                    Intervalos = pares,

                    TotalDia = trabalhado,
                    AjusteAlmoco = ajusteAlmoco,
                    SaidaAlmoco = saidaAlmocoEvt,
                    VoltaAlmoco = voltaAlmocoEvt,

                    HorasExtras = extras,
                    MinutosAtraso = atraso,
                    BancoDeHoras = bancoDia,
                    Ocorrencias = ocorr
                });
            }
        }

        string? empNome = null;
        if (employeeId is > 0)
            empNome = (await _empRepo.GetByIdAsync(employeeId.Value, ct))?.Nome;

        return new WorkSummaryDto(
            inicio,
            fim,
            (employeeId is > 0) ? employeeId : null,
            empNome,
            dias.OrderBy(d => d.Dia).ThenBy(d => d.EmployeeId).ToList(),
            totalPeriodo,
            bancoDoPeriodo
        );
    }

    private static DateTimeOffset ToSp(DateTimeOffset dto, TimeZoneInfo tz)
    {
        var off = tz.GetUtcOffset(dto.UtcDateTime);   // considera DST, se houver
        return dto.ToOffset(off);
    }
}
