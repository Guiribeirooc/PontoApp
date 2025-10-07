using FluentValidation;
using FluentValidation.Validators;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Validation
{
    public class EmployeeFormViewModelValidator : AbstractValidator<EmployeeFormViewModel>
    {
        public EmployeeFormViewModelValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome é obrigatório.")
                .MaximumLength(80);

            RuleFor(x => x.Pin)
                .NotEmpty().WithMessage("O PIN é obrigatório.")
                .Length(4, 6)
                .Matches(@"^\d+$").WithMessage("O PIN deve conter apenas dígitos.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O e-mail é obrigatório.")
                .EmailAddress(EmailValidationMode.Net4xRegex);

            RuleFor(x => x.Cpf)
                .NotEmpty().WithMessage("O CPF é obrigatório.")
                .Must(IsValidCpf).WithMessage("CPF inválido.");

            RuleFor(x => x.BirthDate)
                .Must(d => d <= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("A data de nascimento não pode ser no futuro.")
                .Must(d => d >= DateOnly.FromDateTime(DateTime.Today.AddYears(-110)))
                .WithMessage("A idade não pode ser superior a 110 anos.");

            RuleFor(x => x.ShiftStart)
                .Matches(@"^\d{2}:\d{2}$").When(x => !string.IsNullOrWhiteSpace(x.ShiftStart))
                .WithMessage("Informe no formato HH:mm");
            RuleFor(x => x.ShiftEnd)
                .Matches(@"^\d{2}:\d{2}$").When(x => !string.IsNullOrWhiteSpace(x.ShiftEnd))
                .WithMessage("Informe no formato HH:mm");

            RuleFor(x => x)
                .Must(x => IsEndAfterStart(x.ShiftStart, x.ShiftEnd))
                .When(x => !string.IsNullOrWhiteSpace(x.ShiftStart) && !string.IsNullOrWhiteSpace(x.ShiftEnd))
                .WithMessage("Hora término deve ser maior que hora início.");
        }

        private static bool IsEndAfterStart(string? start, string? end)
        {
            if (!TimeOnly.TryParse(start, out var s)) return true;
            if (!TimeOnly.TryParse(end, out var e)) return true;
            return e > s;
        }

        private static bool IsValidCpf(string? cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf)) return false;
            var digits = new string(cpf.Where(char.IsDigit).ToArray());
            if (digits.Length != 11) return false;
            if (digits.Distinct().Count() == 1) return false;

            int sum = 0;
            for (int i = 0; i < 9; i++) sum += (digits[i] - '0') * (10 - i);
            int r = sum % 11;
            int d1 = r < 2 ? 0 : 11 - r;
            if (d1 != digits[9] - '0') return false;

            sum = 0;
            for (int i = 0; i < 10; i++) sum += (digits[i] - '0') * (11 - i);
            r = sum % 11;
            int d2 = r < 2 ? 0 : 11 - r;
            return d2 == digits[10] - '0';
        }
    }
}