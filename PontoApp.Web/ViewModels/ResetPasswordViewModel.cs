namespace PontoApp.Web.ViewModels
{
    public class ResetPasswordViewModel
    {
        public string Email { get; set; } = "";
        public string Code { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
