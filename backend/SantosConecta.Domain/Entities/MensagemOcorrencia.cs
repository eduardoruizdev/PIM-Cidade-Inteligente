using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Domain.Entities;

public class MensagemOcorrencia
{
    public int IdMensagem { get; set; }
    public int IdOcorrencia { get; set; }
    public int IdRemetente { get; set; }
    [Required, MaxLength(20)] public string TipoRemetente { get; set; } = "";
    [Required, MaxLength(120)] public string NomeRemetente { get; set; } = "";
    [Required, MaxLength(4000)] public string Conteudo { get; set; } = "";
    public DateTime DataEnvio { get; set; }
    public DateTime? DataLeitura { get; set; }
    public Ocorrencia Ocorrencia { get; set; } = null!;
}