using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Application.DTOs;

public class CadastroCidadaoDto : CriarCidadaoDto
{
    [Required, StringLength(128, MinimumLength = 12)] public string Senha { get; set; } = "";
    [Required, Compare(nameof(Senha))] public string ConfirmarSenha { get; set; } = "";
}
public class PerfilCidadaoDto
{
    [Required, StringLength(120, MinimumLength = 2)] public string Nome { get; set; } = "";
    [Required, RegularExpression(@"^\+?[0-9 ()-]{10,20}$")] public string Telefone { get; set; } = "";
}
public class NovaReclamacaoDto
{
    public bool LocalizacaoConfirmada { get; set; } = true;
    [Required, StringLength(10000, MinimumLength = 10)] public string Descricao { get; set; } = "";
    [Required, StringLength(100)] public string Bairro { get; set; } = "";
    [Required, StringLength(200)] public string Endereco { get; set; } = "";
    [Range(-90, 90)] public decimal Latitude { get; set; }
    [Range(-180, 180)] public decimal Longitude { get; set; }
    [Range(1, int.MaxValue)] public int IdCategoria { get; set; }
}
public class RevisarPrioridadeDto
{
    [Range(0,40)] public int Gravidade { get; set; }
    [Range(0,15)] public int ImpactoColetivo { get; set; }
    public bool RiscoImediato { get; set; }
    [Range(0,100)] public int? PontuacaoManual { get; set; }
    [Required, StringLength(350, MinimumLength = 10)] public string Justificativa { get; set; } = "";
}
public record ClassificacaoDto(int? IdCategoria, string? Categoria, int? Gravidade, bool Disponivel, string Aviso);
public class ClassificarDto
{
    [Required, StringLength(3000, MinimumLength = 10)] public string Descricao { get; set; } = "";
}
