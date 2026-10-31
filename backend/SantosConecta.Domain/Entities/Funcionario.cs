using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Domain.Entities;

public class Funcionario
{
    public int IdFuncionario { get; set; }
    [Required, MaxLength(120)] public string Nome { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [Required, MaxLength(255)] public string SenhaHash { get; set; } = "";
    [Required, MaxLength(30)] public string Perfil { get; set; } = "Funcionário";
}
