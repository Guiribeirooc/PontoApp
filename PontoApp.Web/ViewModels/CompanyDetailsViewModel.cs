namespace PontoApp.Web.ViewModels;

public class CompanyDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string CNPJ { get; set; } = "";
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
    public string? LogoBase64 { get; set; }
}
