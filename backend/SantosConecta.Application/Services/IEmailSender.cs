namespace SantosConecta.Application.Services;

public interface IEmailSender
{
    // true = simulado; false = aceito pelo servidor SMTP (não comprova entrega na caixa postal).
    Task<bool> EnviarAsync(string destinatario, string assunto, string mensagem);
}
