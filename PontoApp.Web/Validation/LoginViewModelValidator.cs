using FluentValidation;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Validation
{
    public class LoginViewModelValidator : AbstractValidator<LoginViewModel>
    {
        public LoginViewModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress(FluentValidation.Validators.EmailValidationMode.Net4xRegex);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Informe a senha.");
        }
    }
}