using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Application.DTOs;

public class SalvarFuncionarioDto
{
    [Required, StringLength(120, MinimumLength = 2)] public string Nome { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [Required, RegularExpression(@"^\+?[0-9 ()-]{10,20}$")] public string Telefone { get; set; } = "";
    [Required, MaxLength(100)] public string Cargo { get; set; } = "";
    [StringLength(128, MinimumLength = 12)] public string? Senha { get; set; }
    [Required, RegularExpression("^(Administrador|Funcionário)$")] public string Perfil { get; set; } = "Funcionário";
    public bool Ativo { get; set; } = true;
}