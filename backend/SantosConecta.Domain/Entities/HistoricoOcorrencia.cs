using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Domain.Entities;

public class HistoricoOcorrencia
{
    public int IdHistorico { get; set; }
    public int IdOcorrencia { get; set; }
    public int IdFuncionario { get; set; }
    [Required, MaxLength(500)] public string Descricao { get; set; } = "";
    public DateTime DataAlteracao { get; set; }
    public Ocorrencia Ocorrencia { get; set; } = null!;
    public Funcionario Funcionario { get; set; } = null!;
}
