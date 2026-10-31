using Microsoft.Extensions.Logging;
using SantosConecta.Application.DTOs;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
namespace SantosConecta.Application.Services;

public class EmailService(IEmailRepository emails, IOcorrenciaRepository ocorrencias,
    IEmailSender sender, ILogger<EmailService> logger)
{
    public async Task<List<EmailDto>> HistoricoAsync(int id) =>
        (await emails.PorOcorrenciaAsync(id)).Select(Mapeamento.Dto).ToList();
    public async Task<EmailDto?> EnviarAsync(EnviarEmailDto dto, int autor)
    {
        var o = await ocorrencias.ObterAsync(dto.IdOcorrencia);
        if (o is null) return null;
        if (string.IsNullOrWhiteSpace(dto.Assunto) || string.IsNullOrWhiteSpace(dto.Mensagem))
            throw new ArgumentException("Preencha o assunto e a mensagem.");
        var email = new EmailEnviado
        {
            IdOcorrencia = o.IdOcorrencia,
            IdFuncionario = autor,
            Destinatario = dto.Destinatario.Trim(),
            Assunto = dto.Assunto.Trim(),
            Mensagem = dto.Mensagem.Trim(),
            StatusEnvio = "Pendente"
        };
        // Persistir a tentativa ANTES do SMTP evita perder todo o registro caso o processo pare.
        await emails.AdicionarAsync(email);
        try
        {
            var simulado = await sender.EnviarAsync(email.Destinatario, email.Assunto, email.Mensagem);
            email.StatusEnvio = simulado ? "Simulado" : "Enviado";
            email.DataEnvio = simulado ? null : DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            email.StatusEnvio = "Falhou"; email.DataEnvio = null;
            email.Erro = "O servidor não confirmou o envio. Verifique a configuração SMTP e tente novamente.";
            logger.LogWarning("Falha no envio do e-mail {IdEmail}: {Tipo}", email.IdEmail, ex.GetType().Name);
        }
        await emails.SalvarAsync();
        // O FK carrega o autor ao consultar o histórico; não retornar entidade com SenhaHash.
        return (await emails.PorOcorrenciaAsync(o.IdOcorrencia))
            .Where(e => e.IdEmail == email.IdEmail).Select(Mapeamento.Dto).First();
    }
}
