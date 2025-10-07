namespace PontoApp.Web.Validation;

public static class PasswordRules
{
    public const string StrongPasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,50}$";
    public const string StrongPasswordMessage =
        "A senha deve ter entre 8 e 50 caracteres e conter ao menos 1 letra maiúscula, 1 letra minúscula, 1 número e 1 caractere especial.";
}
