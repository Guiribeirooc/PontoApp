using PontoApp.Domain.Entities;

namespace PontoApp.Web.ViewModels;

public class EspelhoDiaViewModel
{
    public DateOnly Dia { get; set; }
    public List<EspelhoMarcacaoViewModel> Marcas { get; set; } = new();
}

public class EspelhoMarcacaoViewModel
{
    public PunchType Tipo { get; set; }
    public TimeSpan Hora { get; set; }
}