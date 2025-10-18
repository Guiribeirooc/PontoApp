using Microsoft.EntityFrameworkCore;
using PontoApp.Application.DTOs;
using PontoApp.Domain.Entities;
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

public class ReportService : IReportService
{
    private readonly IPunchRepository _punchRepo;
    private readonly IEmployeeRepository _empRepo;
    private readonly WorkRules _rules;

    public ReportService(IPunchRepository punchRepo, IEmployeeRepository empRepo, WorkRules rules)
    {
        _punchRepo = punchRepo;
        _empRepo = empRepo;
        _rules = rules;
    }

    // Recorte de um dia no relógio local (SP): [00:00, 00:00 do dia seguinte)
    private static (DateTime ini, DateTime fimExcl) DiaRange(DateOnly dia)
        => (dia.ToDateTime(TimeOnly.MinValue), dia.AddDays(1).ToDateTime(TimeOnly.MinValue));

    private TimeSpan Round(TimeSpan t)
    {
        var m = _rules.RoundingMinutes;
        if (m <= 1) return t;
        var minutes = Math.Round(t.TotalMinutes / m, MidpointRounding.AwayFromZero) * m;
        return TimeSpan.FromMinutes(minutes);
    }

    private static TimeSpan Previsto(WorkScheduleKind jornada, bool workedToday)
    {
        if (!workedToday) return TimeSpan.Zero;

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

    // Atraso: entrada após início + tolerância; saída antes do fim conta como atraso também
    private (TimeSpan atraso, List<string> ocorr) CalcAtraso(
        DateTime? entrada, DateTime? saida, TimeOnly? sStart, TimeOnly? sEnd)
    {
        var ocorr = new List<string>();
        var atraso = TimeSpan.Zero;

        // tolerância simples de 5 min para entrada
        if (sStart.HasValue && entrada.HasValue)
        {
            var esperadoIn = entrada.Value.Date + sStart.Value.ToTimeSpan();
            var diff = entrada.Value - esperadoIn;
            if (diff > TimeSpan.FromMinutes(5)) atraso += diff;
        }

        if (sEnd.HasValue && saida.HasValue)
        {
            var esperadoOut = saida.Value.Date + sEnd.Value.ToTimeSpan();
            var diff = esperadoOut - saida.Value; // saiu antes -> positivo = atraso
            if (diff > TimeSpan.Zero) atraso += diff;
        }

        return (Round(atraso), ocorr);
    }

    public async Task<WorkSummaryDto> ResumoAsync(
        DateOnly inicio, DateOnly fim, int? employeeId = null, CancellationToken ct = default)
    {
        if (fim < inicio) throw new InvalidOperationException("Período inválido.");

        var (ini, _) = DiaRange(inicio);
        var (_, fimExcl) = DiaRange(fim);

        // Busca já com colaborador (nome/jornada/turno)
        var rows = await (
            from p in _punchRepo.Query().AsNoTracking()
            join e in _empRepo.Query().AsNoTracking() on p.EmployeeId equals e.Id
            where p.DataHora >= ini && p.DataHora < fimExcl
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
            // Agrupa por dia local (sem fuso)
            var porDia = gEmp
                .GroupBy(x => DateOnly.FromDateTime(x.DataHora.Date))
                .OrderBy(g => g.Key);

            foreach (var gDia in porDia)
            {
                var pares = new List<WorkIntervalDto>();
                DateTime? aberto = null;

                // Monta pares Entrada/Volta x Saída/SaídaAlmoço
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

                // Janela "válida" de almoço do dia corrente
                var janelaIni = gDia.Key.ToDateTime(TimeOnly.FromTimeSpan(_rules.LunchWindowStart));
                var janelaFim = gDia.Key.ToDateTime(TimeOnly.FromTimeSpan(_rules.LunchWindowEnd));
                var almMin = TimeSpan.FromMinutes(_rules.MinLunchMinutes);

                // Calcula maior intervalo (gap) dentro da janela de almoço
                TimeSpan maiorGapAlmoco = TimeSpan.Zero;
                DateTime? lastOut = null;

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

                var ajusteAlmoco = (almMin > TimeSpan.Zero && maiorGapAlmoco < almMin)
                    ? (almMin - maiorGapAlmoco)
                    : TimeSpan.Zero;

                // Total trabalhado (somatório dos pares fechados) – aplica ajuste de almoço e teto diário
                var trabalhado = new TimeSpan(pares.Where(i => i.Out != null).Sum(i => i.Duration.Ticks));
                if (ajusteAlmoco > TimeSpan.Zero)
                {
                    trabalhado -= ajusteAlmoco;
                    if (trabalhado < TimeSpan.Zero) trabalhado = TimeSpan.Zero;
                }

                var maxDia = TimeSpan.FromHours(_rules.MaxDailyHours);
                if (maxDia.TotalMinutes > 0 && trabalhado > maxDia) trabalhado = maxDia;

                trabalhado = Round(trabalhado);

                var entrada = pares.FirstOrDefault()?.In;
                var saida = pares.LastOrDefault(i => i.Out != null)?.Out;

                // Atraso
                var (atraso, ocorrAtraso) = CalcAtraso(entrada, saida, gEmp.Key.ShiftStart, gEmp.Key.ShiftEnd);

                // Previsto x banco/extras
                var previsto = Previsto(gEmp.Key.Jornada, workedToday: trabalhado > TimeSpan.Zero);
                var bancoDia = trabalhado - previsto;
                var extras = bancoDia > TimeSpan.Zero ? bancoDia : TimeSpan.Zero;

                var ocorr = new List<string>();
                if (ajusteAlmoco > TimeSpan.Zero)
                    ocorr.Add($"Almoço abaixo de {_rules.MinLunchMinutes} min (ajuste {Round(ajusteAlmoco):hh\\:mm}).");
                ocorr.AddRange(ocorrAtraso);
                if (entrada == null || saida == null)
                    ocorr.Add("Marcações incompletas.");

                totalPeriodo += trabalhado;
                bancoDoPeriodo += bancoDia;

                var saidaAlmocoEvt = gDia.FirstOrDefault(p => p.Tipo == PunchType.SaidaAlmoco)?.DataHora;
                var voltaAlmocoEvt = gDia.FirstOrDefault(p => p.Tipo == PunchType.VoltaAlmoco)?.DataHora;

                dias.Add(new WorkDayDto
                {
                    Dia = gDia.Key,
                    EmployeeId = gEmp.Key.EmployeeId,
                    EmployeeNome = gEmp.Key.Nome,

                    Intervalos = pares,         // já contém Duration arredondada por par
                    TotalDia = trabalhado,    // total do dia (após ajuste/round)
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
}
