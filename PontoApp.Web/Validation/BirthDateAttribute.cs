using System.ComponentModel.DataAnnotations;

namespace PontoApp.Web.Validation
{
    public sealed class BirthDateAttribute(int maxYears = 110) : ValidationAttribute($"Data de nascimento inválida.")
    {
        public int MaxYears { get; } = maxYears;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null) return new ValidationResult("A data de nascimento é obrigatória.");

            DateTime date = value switch
            {
                DateOnly d => d.ToDateTime(TimeOnly.MinValue),
                DateTime dt => dt.Date,
                _ => DateTime.MinValue
            };
            if (date == DateTime.MinValue) return new ValidationResult("Data de nascimento inválida.");

            var today = DateTime.Today;
            if (date > today) return new ValidationResult("A data de nascimento não pode ser no futuro.");

            var min = today.AddYears(-MaxYears);
            if (date < min) return new ValidationResult($"A idade não pode ser superior a {MaxYears} anos.");

            return ValidationResult.Success;
        }
    }
}
