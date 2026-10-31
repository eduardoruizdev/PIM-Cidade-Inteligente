using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Domain.Entities;

public class Ocorrencia
{
    public int IdOcorrencia { get; set; }
    [Required, MaxLength(30)] public string Protocolo { get; set; } = "";
    [Required, MaxLength(150)] public string Titulo { get; set; } = "";
    [Required] public string Descricao { get; set; } = "";
    [Required, MaxLength(100)] public string Bairro { get; set; } = "";
    [Required, MaxLength(200)] public string Endereco { get; set; } = "";
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
