using FluentValidation;
using FluentValidation.Validators;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Validation
{
    public class AdminBootstrapViewModelValidator : AbstractValidator<AdminBootstrapViewModel>
    {
        public AdminBootstrapViewModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress(EmailValidationMode.Net4xRegex);

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