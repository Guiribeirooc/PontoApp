using System.ComponentModel.DataAnnotations;

namespace PontoApp.Web.ViewModels
{
    public class CompanyInviteViewModel
    {
        [Display(Name = "Chave de Acesso"), Required]
        public string AccessKey { get; set; } = string.Empty;

        [Display(Name = "Nome/Razão Social"), Required, StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "CNPJ"), Required, RegularExpression(@"^\d{14}$", ErrorMessage = "CNPJ deve ter 14 dígitos numéricos")]
        public string CNPJ { get; set; } = string.Empty;

        [Display(Name = "Validade (horas)")]
        [Range(1, 168, ErrorMessage = "Informe um valor entre 1 e 168 horas.")]
        public int? ValidityHours { get; set; } = 48;

        [Display(Name = "Número máximo de usos")]
        [Range(1, 10, ErrorMessage = "Informe um valor entre 1 e 10.")]
        public int? MaxUses { get; set; } = 1;

        public string? GeneratedLink { get; set; }
        public DateTime? GeneratedExpiresAt { get; set; }
    }
}
