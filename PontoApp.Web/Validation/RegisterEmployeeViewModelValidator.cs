using FluentValidation;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Validation
{
    public class RegisterEmployeeViewModelValidator : AbstractValidator<RegisterEmployeeViewModel>
    {
        public RegisterEmployeeViewModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Informe o e-mail.")
                .EmailAddress(FluentValidation.Validators.EmailValidationMode.Net4xRegex)
                .WithMessage("E-mail inválido.");

            RuleFor(x => x.Pin)
                .NotEmpty().WithMessage("Informe o PIN.")
                .MaximumLength(10);

            RuleFor(x => x.Password)
                .NotEmpty()
                .Matches(PasswordRules.StrongPasswordRegex)
                .WithMessage(PasswordRules.StrongPasswordMessage);

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password)
                .WithMessage("As senhas não conferem.");
        }
    }
}