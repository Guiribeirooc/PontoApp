using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Domain.Enums;
using PontoApp.Domain.Interfaces;

namespace PontoApp.Application.Services;

public interface IPunchService
{
    Task<PunchResultDto> MarcarAsync(string nome, string pin, PunchType tipo, string? ip, CancellationToken ct = default);
    Task<List<PunchResultDto>> ListarDoDiaAsync(DateOnly dia, int? employeeId = null, CancellationToken ct = default);
    Task<List<PunchResultDto>> ListarPeriodoAsync(DateOnly inicio, DateOnly fim, int? employeeId = null, CancellationToken ct = default);
    Task<PunchResultDto> MarcarManualAsync(int employeeId, PunchType tipo, DateTimeOffset dataHora, string justificativa, CancellationToken ct = default);
    Task<IEnumerable<PunchResultDto>> GetAllDoDiaAsync(DateOnly dia, CancellationToken ct = default);
}

public class PunchService(IEmployeeRepository empRepo, IPunchRepository punchRepo, IClock clock) : IPunchService
{
    private readonly IEmployeeRepository _empRepo = empRepo;
    private readonly IPunchRepository _punchRepo = punchRepo;
    private readonly IClock _clock = clock;

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
        var fim = ini.AddDays(1);
        return (ini, fim);
    }

    public async Task<PunchResultDto> MarcarAsync(string nome, string pin, PunchType tipo, string? ip, CancellationToken ct = default)
    {
        var emp = await _empRepo.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Pin == pin && e.Ativo && !e.IsDeleted, ct)
            ?? throw new InvalidOperationException("Colaborador(a) não encontrado(a) ou PIN inválido.");

        var now = _clock.NowSp();

        bool lunchMandatory =
            emp.Jornada != WorkScheduleKind.Parcial &&
            emp.Jornada != WorkScheduleKind.Intermitente &&
            emp.Jornada != WorkScheduleKind.Estagiario;

        var tz = GetBrTz();
        var (ini, fim) = DiaRange(DateOnly.FromDateTime(now.Date), tz);

        var punchesHoje = await _punchRepo.Query()
            .Where(p => p.EmployeeId == emp.Id && p.DataHora >= ini && p.DataHora < fim)
            .OrderBy(p => p.DataHora)
            .ToListAsync(ct);

        if (tipo == PunchType.Entrada && punchesHoje.Any(p => p.Tipo == PunchType.Entrada))
            throw new InvalidOperationException("Já existe uma marcação de Entrada para hoje.");

        if (tipo == PunchType.SaidaAlmoco && punchesHoje.Any(p => p.Tipo == PunchType.SaidaAlmoco))
            throw new InvalidOperationException("Já existe uma marcação de Saída para Almoço para hoje.");

        if (tipo == PunchType.VoltaAlmoco && punchesHoje.Any(p => p.Tipo == PunchType.VoltaAlmoco))
            throw new InvalidOperationException("Já existe uma marcação de Volta do Almoço para hoje.");

        if (tipo == PunchType.Saida && punchesHoje.Any(p => p.Tipo == PunchType.Saida))
            throw new InvalidOperationException("Já existe uma marcação de Saída para hoje.");

        var minLunch = TimeSpan.FromMinutes(60);

        if (tipo == PunchType.SaidaAlmoco)
        {
            var ultimaEntradaOuVolta = punchesHoje
                .Where(p => p.Tipo == PunchType.Entrada || p.Tipo == PunchType.VoltaAlmoco)
                .LastOrDefault() ?? throw new InvalidOperationException("Não é possível registrar Saída para Almoço sem uma Entrada/Volta do Almoço anterior no dia.");

            var diff = now - ultimaEntradaOuVolta.DataHora;
            if (diff < minLunch)
                throw new InvalidOperationException($"A Saída para Almoço só pode ser registrada após {minLunch.TotalMinutes:0} minutos da última Entrada/Volta.");
        }

        if (tipo == PunchType.VoltaAlmoco)
        {
            var ultimaSaidaAlmoco = punchesHoje.LastOrDefault(p => p.Tipo == PunchType.SaidaAlmoco)
                ?? throw new InvalidOperationException("Não é possível registrar Volta do Almoço sem uma Saída para Almoço anterior no dia.");

            var diff = now - ultimaSaidaAlmoco.DataHora;
            if (diff < minLunch)
                throw new InvalidOperationException($"A Volta do Almoço só pode ser registrada após {minLunch.TotalMinutes:0} minutos da Saída para Almoço.");
        }

        if (tipo == PunchType.Saida && lunchMandatory)
        {
            var saidaAlmoco = punchesHoje.LastOrDefault(p => p.Tipo == PunchType.SaidaAlmoco);
            var voltaAlmoco = punchesHoje.LastOrDefault(p => p.Tipo == PunchType.VoltaAlmoco);

            if (saidaAlmoco is null && voltaAlmoco is null)
                throw new InvalidOperationException("Para sua jornada, é obrigatório registrar a Saída e a Volta do Almoço antes de registrar a Saída.");

            if (voltaAlmoco is null)
                throw new InvalidOperationException("Para sua jornada, é obrigatório registrar a Volta do Almoço antes da Saída.");
        }

        var punch = new Punch
        {
            EmployeeId = emp.Id,
            Tipo = tipo,
            DataHora = now,
            Ip = ip
        };

        await _punchRepo.AddAsync(punch, ct);
        await _punchRepo.SaveChangesAsync(ct);

        return new PunchResultDto(
            punch.Id,
            emp.Id,
            emp.Pin,
            emp.Nome,
            punch.Tipo,
            punch.DataHora
        );
    }

    public async Task<PunchResultDto> MarcarManualAsync(
        int employeeId,
        PunchType tipo,
        DateTimeOffset dataHora,
        string justificativa,
        CancellationToken ct = default)
    {
        var emp = await _empRepo.GetByIdAsync(employeeId, ct);
        if (emp is null || !emp.Ativo || emp.IsDeleted)
            throw new InvalidOperationException("Colaborador inválido ou inativo.");

        if (string.IsNullOrWhiteSpace(justificativa))
            throw new InvalidOperationException("Justificativa é obrigatória para marcações manuais.");

        var existeIgual = await _punchRepo.ExistsAsync(employeeId, tipo, dataHora, ct);
        if (existeIgual)
            throw new InvalidOperationException("Já existe uma marcação idêntica neste instante.");

        var punch = new Punch
        {
            EmployeeId = employeeId,
            DataHora = dataHora,
            Tipo = tipo,
            Justificativa = justificativa,
            Origem = "Manual(Admin)"
        };

        await _punchRepo.AddAsync(punch, ct);
        await _punchRepo.SaveChangesAsync(ct);

        return new PunchResultDto(
            punch.Id,
            EmployeeId: emp.Id,
            EmployeePin: emp.Pin,
            EmployeeNome: emp.Nome,
            Tipo: tipo,
            DataHora: dataHora
        );
    }

    public async Task<List<PunchResultDto>> ListarDoDiaAsync(DateOnly dia, int? employeeId = null, CancellationToken ct = default)
    {
        var tz = GetBrTz();
        var (ini, fim) = DiaRange(dia, tz);

        var q =
            from p in _punchRepo.Query()
            join e in _empRepo.Query() on p.EmployeeId equals e.Id
            where p.DataHora >= ini && p.DataHora < fim
                  && (employeeId == null || p.EmployeeId == employeeId.Value)
                  && e.Ativo && !e.IsDeleted
            orderby e.Nome, p.DataHora
            select new PunchResultDto(
                p.Id,
                e.Id,
                e.Pin,
                e.Nome,
                p.Tipo,
                p.DataHora
            );

        return await q.AsNoTracking().ToListAsync(ct);
    }

    public async Task<List<PunchResultDto>> ListarPeriodoAsync(DateOnly inicio, DateOnly fim, int? employeeId = null, CancellationToken ct = default)
    {
        var tz = GetBrTz();
        var ini = DiaRange(inicio, tz).ini;
        var end = DiaRange(fim, tz).fim;

        var q =
            from p in _punchRepo.Query()
            join e in _empRepo.Query() on p.EmployeeId equals e.Id
            where p.DataHora >= ini && p.DataHora < end
                  && (employeeId == null || p.EmployeeId == employeeId.Value)
                  && e.Ativo && !e.IsDeleted
            orderby e.Nome, p.DataHora
            select new PunchResultDto(
                p.Id,
                e.Id,
                e.Pin,
                e.Nome,
                p.Tipo,
                p.DataHora
            );

        return await q.AsNoTracking().ToListAsync(ct);
    }

    public async Task<IEnumerable<PunchResultDto>> GetAllDoDiaAsync(DateOnly dia, CancellationToken ct = default)
    {
        return await ListarDoDiaAsync(dia, null, ct);
    }
}
