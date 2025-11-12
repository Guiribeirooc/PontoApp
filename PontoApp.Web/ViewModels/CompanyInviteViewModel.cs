using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PontoApp.Web.ViewModels;

public class CompanyInviteViewModel : IValidatableObject
{
    [Display(Name = "Chave de Acesso")]
    public string? AccessKey { get; set; } // Validada no controller se MasterKey estiver configurada

    [Display(Name = "Nome/Razão Social"), Required, StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Display(Name = "CNPJ"), Required]
    public string CNPJ { get; set; } = string.Empty;

    [Display(Name = "Validade (horas)")]
    [Range(1, 168, ErrorMessage = "Informe um valor entre 1 e 168 horas.")]
    public int? ValidityHours { get; set; } = 48;

    [Display(Name = "Número máximo de usos")]
    [Range(1, 10, ErrorMessage = "Informe um valor entre 1 e 10.")]
    public int? MaxUses { get; set; } = 1;

    public string? GeneratedLink { get; set; }
    public DateTime? GeneratedExpiresAt { get; set; }
    public string? GeneratedToken { get; set; }

    // Normaliza: mantém só dígitos do CNPJ informado (com ou sem máscara)
    public string CnpjDigits => new string((CNPJ ?? string.Empty).Where(char.IsDigit).ToArray());

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(CNPJ))
            yield break; // [Required] já cobre

        if (CnpjDigits.Length != 14)
        {
            yield return new ValidationResult(
                "CNPJ deve conter 14 dígitos (com ou sem máscara).",
                new[] { nameof(CNPJ) });
        }
    }
}
