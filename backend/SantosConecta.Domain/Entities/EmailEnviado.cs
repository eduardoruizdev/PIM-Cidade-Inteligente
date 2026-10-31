using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Domain.Entities;

public class EmailEnviado
{
    public int IdEmail { get; set; }
    public int IdOcorrencia { get; set; }
    public int IdFuncionario { get; set; }
    [Required, EmailAddress, MaxLength(150)] public string Destinatario { get; set; } = "";
    [Required, MaxLength(200)] public string Assunto { get; set; } = "";
    [Required] public string Mensagem { get; set; } = "";
    public DateTime? DataEnvio { get; set; }
    [Required, MaxLength(30)] public string StatusEnvio { get; set; } = "Pendente";
    [MaxLength(500)] public string? Erro { get; set; }
    public Ocorrencia Ocorrencia { get; set; } = null!;
    public Funcionario Funcionario { get; set; } = null!;
}
