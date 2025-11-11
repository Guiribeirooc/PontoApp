using System.ComponentModel.DataAnnotations;

namespace PontoApp.Web.ViewModels;

public class CompanyEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "Nome/Razão Social"), Required, StringLength(200)]
    public string Name { get; set; } = "";

    [Display(Name = "CNPJ"), Required, RegularExpression(@"^\d{14}$")]
    public string CNPJ { get; set; } = "";

    [Display(Name = "Inscrição Estadual")] public string? IE { get; set; }
    [Display(Name = "Inscrição Municipal")] public string? IM { get; set; }

    [Display(Name = "Logradouro")] public string? Logradouro { get; set; }
    [Display(Name = "Número")] public string? Numero { get; set; }
    [Display(Name = "Complemento")] public string? Complemento { get; set; }
    [Display(Name = "Bairro")] public string? Bairro { get; set; }
    [Display(Name = "Cidade")] public string? Cidade { get; set; }
    [Display(Name = "UF"), StringLength(2)] public string? UF { get; set; }
    [Display(Name = "CEP"), RegularExpression(@"^\d{8}$")] public string? CEP { get; set; }
    [Display(Name = "País")] public string? Pais { get; set; }

    [Display(Name = "Telefone")] public string? Telefone { get; set; }
    [Display(Name = "E-mail de Contato"), EmailAddress] public string? EmailContato { get; set; }

    [Display(Name = "Logo (opcional)")] public IFormFile? Logo { get; set; }
    public string? ExistingLogoBase64 { get; set; }
    public string? Active { get; set; }
}
