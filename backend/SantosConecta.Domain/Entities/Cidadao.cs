using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Domain.Entities;

public class Cidadao
{
    public int IdCidadao { get; set; }
    [Required, MaxLength(120)] public string Nome { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [MaxLength(20)] public string Telefone { get; set; } = "";
}
