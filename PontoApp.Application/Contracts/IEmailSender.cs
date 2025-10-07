namespace PontoApp.Application.Contracts
{
    public interface IEmailSender
    {
        Task<(bool Success, string Message)> SendAsync(
                        string toEmail,
                        string subject,
                        string htmlBody,
                        CancellationToken ct = default);
    }
}
