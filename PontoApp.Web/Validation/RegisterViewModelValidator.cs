using FluentValidation;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Validation
{
    public class RegisterViewModelValidator : AbstractValidator<RegisterViewModel>
    {
        public RegisterViewModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress(FluentValidation.Validators.EmailValidationMode.Net4xRegex);

            RuleFor(x => x.Password)
                .NotEmpty()
                .Matches(PasswordRules.StrongPasswordRegex)
                .WithMessage(PasswordRules.StrongPasswordMessage);
        }
    }
}