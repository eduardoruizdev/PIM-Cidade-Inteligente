using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Application.DTOs;

public record CidadaoDto(int IdCidadao, string Nome, string Email, string Telefone);
public record FuncionarioDto(int IdFuncionario, string Nome, string Email, string Perfil, string Telefone = "", string Cargo = "", bool Ativo = true);
public record CategoriaDto(int IdCategoria, string Nome);
public class CriarCidadaoDto
{
    [Required, StringLength(120, MinimumLength = 2)] public string Nome { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [MaxLength(20)] public string Telefone { get; set; } = "";
}
public class LoginDto
{
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [Required, MinLength(8), MaxLength(128)] public string Senha { get; set; } = "";
}
