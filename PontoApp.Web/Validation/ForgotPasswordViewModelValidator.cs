using FluentValidation;
using FluentValidation.Validators;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Validation
{
    public class ForgotPasswordViewModelValidator : AbstractValidator<ForgotPasswordViewModel>
    {
        public ForgotPasswordViewModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress(EmailValidationMode.Net4xRegex);
        }
    }
}