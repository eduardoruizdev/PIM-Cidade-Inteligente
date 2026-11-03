using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Domain.Entities;

public class IndicadorTerritorial
{
    public int IdIndicador { get; set; }
    [Required, MaxLength(100)] public string Bairro { get; set; } = "";
    [Range(0, 100)] public int IndiceVulnerabilidade { get; set; }
    [Required, MaxLength(200)] public string Fonte { get; set; } = "SIMULAÇÃO ACADÊMICA";
    public int AnoReferencia { get; set; }
    [Required, MaxLength(60)] public string TipoDado { get; set; } = "SIMULAÇÃO ACADÊMICA";
}
