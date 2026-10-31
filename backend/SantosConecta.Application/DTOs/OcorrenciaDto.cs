using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Application.DTOs;

public record HistoricoDto(int IdHistorico, string Descricao, DateTime DataAlteracao, string Funcionario);
public record OcorrenciaDto(int IdOcorrencia, string Protocolo, string Titulo, string Descricao,
    string Bairro, string Endereco, decimal Latitude, decimal Longitude, DateTime DataAbertura,
    string Status, string Prioridade, CidadaoDto Cidadao, FuncionarioDto? Funcionario,
    CategoriaDto Categoria, List<HistoricoDto> Historico);
public class SalvarOcorrenciaDto
{
    [Required, StringLength(150, MinimumLength = 5)] public string Titulo { get; set; } = "";
    [Required, StringLength(10000, MinimumLength = 10)] public string Descricao { get; set; } = "";
    [Required, MaxLength(100)] public string Bairro { get; set; } = "";
    [Required, MaxLength(200)] public string Endereco { get; set; } = "";
    [Range(-90, 90)] public decimal Latitude { get; set; }
    [Range(-180, 180)] public decimal Longitude { get; set; }
    [Required, RegularExpression("^(Aberta|Em análise|Em andamento|Resolvida|Cancelada)$")]
    public string Status { get; set; } = "Aberta";
    [Required, RegularExpression("^(Baixa|Média|Alta|Crítica)$")]
    public string Prioridade { get; set; } = "Média";
    [Range(1, int.MaxValue)] public int IdCidadao { get; set; }
    [Range(1, int.MaxValue)] public int? IdFuncionario { get; set; }
    [Range(1, int.MaxValue)] public int IdCategoria { get; set; }
}
public class AtualizarStatusDto
{
    [Required, RegularExpression("^(Aberta|Em análise|Em andamento|Resolvida|Cancelada)$")]
    public string Status { get; set; } = "";
}
public class ObservacaoDto
{
    [Required, StringLength(500, MinimumLength = 3)] public string Descricao { get; set; } = "";
}
public record DashboardResumoDto(int Total, int AguardandoAnalise, int EmAndamento, int Resolvidas);
public record BairroDto(string Bairro, int Quantidade);
