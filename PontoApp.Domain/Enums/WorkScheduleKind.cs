namespace PontoApp.Domain.Enums
{
    public enum WorkScheduleKind
    {
        Integral = 0,        // 8h/dia, 44h/sem
        Parcial = 1,         // carga reduzida (ex.: até 6h/dia)
        DozePorTrintaSeis = 2, // 12x36
        Noturna = 3,         // 22h às 5h
        Remota = 4,          // home office (mesmas regras de hora)
        Intermitente = 5,    // por convocações
        Estagiario = 6       // até 6h/dia e 30h/sem (regra típica)
    }
}