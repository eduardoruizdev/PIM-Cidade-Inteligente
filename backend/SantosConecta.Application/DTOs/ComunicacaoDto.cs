using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Application.DTOs;

public class EnviarEmailDto
{
    [Range(1, int.MaxValue)] public int IdOcorrencia { get; set; }
    [Required, EmailAddress, MaxLength(150)] public string Destinatario { get; set; } = "";
    [Required, StringLength(200, MinimumLength = 3)] public string Assunto { get; set; } = "";
    [Required, StringLength(10000, MinimumLength = 10)] public string Mensagem { get; set; } = "";
}
public record EmailDto(int IdEmail, int IdOcorrencia, string Destinatario, string Assunto,
    string Mensagem, DateTime? DataEnvio, string StatusEnvio, string? Erro, string Funcionario);
public class GerarRespostaDto
{
    [Range(1, int.MaxValue)] public int IdOcorrencia { get; set; }
}
public record SugestaoDto(string Assunto, string Mensagem, bool Simulada);
