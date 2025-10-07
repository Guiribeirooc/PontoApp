using FluentValidation;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Validation
{
    public class ResetPasswordViewModelValidator : AbstractValidator<ResetPasswordViewModel>
    {
        public ResetPasswordViewModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress(FluentValidation.Validators.EmailValidationMode.Net4xRegex);

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Informe o código recebido por e-mail.");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .Matches(PasswordRules.StrongPasswordRegex)
                .WithMessage(PasswordRules.StrongPasswordMessage);

        }
    }
}