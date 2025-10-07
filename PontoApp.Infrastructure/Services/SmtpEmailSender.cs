using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using PontoApp.Application.Contracts;

namespace PontoApp.Infrastructure.Services
{
    public class SmtpEmailSender(IConfiguration cfg) : IEmailSender
    {
        private readonly IConfiguration _cfg = cfg;

        public async Task<(bool Success, string Message)> SendAsync(
                string toEmail,
                string subject,
                string htmlBody,
                CancellationToken ct = default)
        {
            try
            {
                var s = _cfg.GetSection("Email");

                var fromName = s["FromName"]!;
                var fromAddr = s["FromAddress"]!;
                var host = s["SmtpHost"]!;
                var port = int.Parse(s["SmtpPort"]!);
                var user = s["Username"]!;
                var pass = s["Password"]!;
                var useStartTls = bool.Parse(s["UseStartTls"] ?? "true");

                using var msg = new MailMessage();
                msg.From = new MailAddress(fromAddr, fromName);
                msg.To.Add(toEmail);
                msg.Subject = subject;
                msg.Body = htmlBody;
                msg.IsBodyHtml = true;

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = useStartTls,
                    Credentials = new NetworkCredential(user, pass)
                };

                await client.SendMailAsync(msg, ct);

                return (true, $"E-mail enviado com sucesso para {toEmail}");
            }
            catch (SmtpException ex)
            {
                return (false, $"Erro SMTP: {ex.StatusCode} - {ex.Message}");
            }
            catch (FormatException ex)
            {
                return (false, $"Formato inválido de e-mail: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Erro inesperado ao enviar e-mail: {ex.Message}");
            }
        }
    }
}