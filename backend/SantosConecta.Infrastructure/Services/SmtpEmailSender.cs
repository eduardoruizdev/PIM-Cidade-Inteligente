using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using SantosConecta.Application.Services;
namespace SantosConecta.Infrastructure.Services;

public class SmtpEmailSender(IConfiguration config) : IEmailSender
{
    public async Task<bool> EnviarAsync(string destinatario, string assunto, string mensagem)
    {
        if (config.GetValue("Email:Simular", true)) return true;
        var host = config["SMTP_HOST"];
        var remetente = config["SMTP_FROM"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(remetente))
            throw new InvalidOperationException("Configure SMTP_HOST e SMTP_FROM no servidor.");
        using var email = new MailMessage(remetente, destinatario, assunto, mensagem) { IsBodyHtml = false };
        using var smtp = new SmtpClient(host, config.GetValue("SMTP_PORT", 587))
        {
            EnableSsl = config.GetValue("SMTP_ENABLE_SSL", true),
            UseDefaultCredentials = false,
            Timeout = 15000
        };
        if (!string.IsNullOrWhiteSpace(config["SMTP_USER"]))
            smtp.Credentials = new NetworkCredential(config["SMTP_USER"], config["SMTP_PASSWORD"]);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await smtp.SendMailAsync(email, timeout.Token);
        return false;
    }
}
