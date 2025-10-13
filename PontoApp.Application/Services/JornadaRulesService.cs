using PontoApp.Domain.Enums;

namespace PontoApp.Application.Services;

internal static class JornadaRulesService
{
    public static readonly TimeSpan ToleranciaAtraso = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan AlmoçoMinimo = TimeSpan.FromMinutes(60);
    public static readonly TimeSpan Arredondamento = TimeSpan.FromMinutes(5);

    public static TimeSpan Previsto(WorkScheduleKind j, bool houveTrabalhoHoje)
    {
        return j switch
        {
            WorkScheduleKind.Integral => TimeSpan.FromHours(8),
            WorkScheduleKind.Parcial => TimeSpan.FromHours(6),
            WorkScheduleKind.Noturna => TimeSpan.FromHours(7), // exemplo
            WorkScheduleKind.Remota => TimeSpan.FromHours(8),
            WorkScheduleKind.Intermitente => TimeSpan.Zero,
            WorkScheduleKind.Estagiario => TimeSpan.FromHours(6),
            WorkScheduleKind.DozePorTrintaSeis
                => houveTrabalhoHoje ? TimeSpan.FromHours(12) : TimeSpan.Zero,
            _ => TimeSpan.Zero
        };
    }
}
