using PontoApp.Domain.Enums;

namespace PontoApp.Web.ViewModels;

public class EmployeeDetailsViewModel
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string? PhotoUrl { get; set; }

    // Pessoais
    public string? Cpf { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? NisPis { get; set; }
    public string TimeZoneDisplay { get; set; } = "São Paulo, Brasil";

    // Alocação
    public string? Cidade { get; set; }
    public string? Estado { get; set; }

    // Contratuais
    public string? Departamento { get; set; }
    public string? Cargo { get; set; }
    public string? Matricula { get; set; }
    public decimal? ValorHora { get; set; }

    // Cálculo de ponto
    public DateOnly? Admissao { get; set; }
    public bool BancoHorasHabilitado { get; set; }
    public DateOnly? InicioRegistro { get; set; }
    public DateOnly? FimRegistro { get; set; }
    public DateOnly? InicioAquisitivoFerias { get; set; }
    public string? Gestor { get; set; }
    public string? Empregador { get; set; }
    public string? Unidade { get; set; }

    // Jornada/Escala
    public WorkScheduleKind Jornada { get; set; }
    public TimeOnly? ShiftStart { get; set; }
    public TimeOnly? ShiftEnd { get; set; }
}
