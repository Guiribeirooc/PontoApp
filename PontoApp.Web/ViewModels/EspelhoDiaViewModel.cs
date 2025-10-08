namespace PontoApp.Web.ViewModels;

public class EspelhoDiaViewModel
{
    public DateOnly Dia { get; set; }
    public List<MarcaVm> Marcas { get; set; } = new();
}

public class MarcaVm
{
    public PunchType Tipo { get; set; }
    public DateTime Hora { get; set; } // usamos DateTime na View
}