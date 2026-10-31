using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Domain.Entities;

public class Categoria
{
    public int IdCategoria { get; set; }
    [Required, MaxLength(100)] public string Nome { get; set; } = "";
}
