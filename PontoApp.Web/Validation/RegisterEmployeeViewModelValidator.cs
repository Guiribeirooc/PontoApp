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
                .Length(4, 10).WithMessage("O PIN deve ter entre {MinLength} e {MaxLength} caracteres.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Informe a senha.")
                .Matches(PasswordRules.StrongPasswordRegex)
                .WithMessage(PasswordRules.StrongPasswordMessage);

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirme a senha.")
                .Equal(x => x.Password)
                .WithMessage("As senhas não conferem.");
        }
    }
}