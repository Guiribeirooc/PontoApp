namespace PontoApp.Domain.Entities;

public class WorkRule
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    public string Nome { get; set; } = null!;
    public int CargaDiariaMin { get; set; }   // minutos/dia
    public int ToleranciaMin { get; set; } = 0;
}