using FluentValidation;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Validation
{
    public class PunchMarkViewModelValidator : AbstractValidator<PunchMarkViewModel>
    {
        public PunchMarkViewModelValidator()
        {
            RuleFor(x => x.EmployeeId)
                .NotNull().WithMessage("Selecione o colaborador");

            RuleFor(x => x.Tipo)
                .IsInEnum().WithMessage("Selecione o tipo");
        }
    }
}