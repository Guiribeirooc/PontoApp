using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace PontoApp.Web.Validation
{
    public sealed class StrictEmailAttribute : ValidationAttribute
    {
        private static readonly Regex DomainRegex =
            new(@"^[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?)+$",
                RegexOptions.Compiled);

        public StrictEmailAttribute() : base("E-mail inválido.") { }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var email = value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(email)) return new ValidationResult(ErrorMessage);

            try
            {
                var addr = new MailAddress(email);
                if (!DomainRegex.IsMatch(addr.Host)) return new ValidationResult(ErrorMessage);
                return ValidationResult.Success;
            }
            catch
            {
                return new ValidationResult(ErrorMessage);
            }
        }
    }
}
