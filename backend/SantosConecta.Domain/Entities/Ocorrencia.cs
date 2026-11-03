using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Domain.Entities;

public class Ocorrencia
{
    public int IdOcorrencia { get; set; }
    [Range(0, 40)] public int Gravidade { get; set; } = 20;
    [Range(0, 15)] public int ImpactoColetivo { get; set; } = 5;
    public bool RiscoImediato { get; set; }
    [Range(0, 100)] public int? PontuacaoManual { get; set; }
    [MaxLength(500)] public string? JustificativaPrioridade { get; set; }
    [Required, MaxLength(30)] public string Protocolo { get; set; } = "";
    [Required, MaxLength(150)] public string Titulo { get; set; } = "";
    [Required] public string Descricao { get; set; } = "";
    [Required, MaxLength(100)] public string Bairro { get; set; } = "";
    [Required, MaxLength(200)] public string Endereco { get; set; } = "";
    public bool LocalizacaoConfirmada { get; set; } = true;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime DataAbertura { get; set; }
    [Required, MaxLength(30)] public string Status { get; set; } = "Aberta";
    [Required, MaxLength(20)] public string Prioridade { get; set; } = "Média";
    public int IdCidadao { get; set; }
    public int? IdFuncionario { get; set; }
    public int IdCategoria { get; set; }
    public Cidadao Cidadao { get; set; } = null!;
    public Funcionario? Funcionario { get; set; }
    public Categoria Categoria { get; set; } = null!;
    public List<HistoricoOcorrencia> Historico { get; set; } = [];
}
