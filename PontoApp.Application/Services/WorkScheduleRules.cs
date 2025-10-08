using PontoApp.Domain.Enums;

namespace PontoApp.Application.Services
{
    public static class WorkScheduleRules
    {
        public static (double DailyMax, double WeeklyMax) Caps(WorkScheduleKind k) => k switch
        {
            WorkScheduleKind.Integral => (8, 44),
            WorkScheduleKind.Parcial => (6, 30),
            WorkScheduleKind.DozePorTrintaSeis => (12, 36),
            WorkScheduleKind.Noturna => (7, 44),
            WorkScheduleKind.Remota => (8, 44),
            WorkScheduleKind.Intermitente => (12, 44),
            WorkScheduleKind.Estagiario => (6, 30),
            _ => (8, 44)
        };
    }
}
