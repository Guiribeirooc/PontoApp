using FluentValidation;
using System.Globalization;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Validation
{
    public class AdminManualPunchViewModelValidator : AbstractValidator<AdminManualPunchViewModel>
    {
        public AdminManualPunchViewModelValidator()
        {
            RuleFor(x => x.EmployeeId)
                .NotNull().WithMessage("Selecione o colaborador.");

            RuleFor(x => x.Tipo)
                .IsInEnum().WithMessage("Selecione o tipo.");

            RuleFor(x => x.DataHoraLocal)
                .NotEmpty().WithMessage("Informe a data/hora.")
                .Must(s => DateTime.TryParseExact(
                        s,
                        "yyyy-MM-ddTHH:mm",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out _))
                .WithMessage("Data/hora inválida.");

            RuleFor(x => x.Justificativa)
                .NotEmpty()
                .MaximumLength(300);
        }
    }
}