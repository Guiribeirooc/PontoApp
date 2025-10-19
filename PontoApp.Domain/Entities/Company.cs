namespace PontoApp.Domain.Entities;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string CNPJ { get; set; } = null!;
    public string? IE { get; set; }
    public string? IM { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? UF { get; set; }
    public string? CEP { get; set; }
    public string? Pais { get; set; }
    public string? Telefone { get; set; }
    public string? EmailContato { get; set; }
    public byte[]? LogoBytes { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
