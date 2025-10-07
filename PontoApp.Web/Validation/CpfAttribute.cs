using System.ComponentModel.DataAnnotations;

namespace PontoApp.Web.Validation
{
    public sealed class CpfAttribute : ValidationAttribute
    {
        public CpfAttribute() : base("CPF inválido.") { }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var s = value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(s)) return new ValidationResult(ErrorMessage);

            var digits = new string(s.Where(char.IsDigit).ToArray());
            if (digits.Length != 11) return new ValidationResult(ErrorMessage);
            if (digits.Distinct().Count() == 1) return new ValidationResult(ErrorMessage);

            int sum = 0;
            for (int i = 0; i < 9; i++) sum += (digits[i] - '0') * (10 - i);
            int r = sum % 11;
            int d1 = r < 2 ? 0 : 11 - r;
            if (d1 != (digits[9] - '0')) return new ValidationResult(ErrorMessage);

            sum = 0;
            for (int i = 0; i < 10; i++) sum += (digits[i] - '0') * (11 - i);
            r = sum % 11;
            int d2 = r < 2 ? 0 : 11 - r;
            if (d2 != (digits[10] - '0')) return new ValidationResult(ErrorMessage);

            return ValidationResult.Success;
        }
    }
}
