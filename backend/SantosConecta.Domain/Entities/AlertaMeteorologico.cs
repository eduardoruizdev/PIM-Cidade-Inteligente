using System.ComponentModel.DataAnnotations;
namespace SantosConecta.Domain.Entities;

public class AlertaMeteorologico
{
    [MaxLength(128)] public string Id { get; set; } = "";
    [MaxLength(40)] public string Origem { get; set; } = "Automatico";
    [MaxLength(160)] public string Fonte { get; set; } = "Santos Conector / Open-Meteo";
    [MaxLength(240)] public string Titulo { get; set; } = "";
    [MaxLength(4000)] public string Descricao { get; set; } = "";
    [MaxLength(40)] public string Severidade { get; set; } = "Preventivo";
    [MaxLength(200)] public string Regiao { get; set; } = "Santos/SP (ponto de previsão)";
    [MaxLength(1000)] public string Referencia { get; set; } = "https://open-meteo.com/en/docs";
    public DateTime PublicadoEm { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public bool Ativo { get; set; } = true;
}
public class PreferenciaMeteorologica
{
    public int IdCidadao { get; set; }
    public bool Habilitada { get; set; }
    [MaxLength(2048)] public string? Token { get; set; }
    public DateTime AtualizadaEm { get; set; }
}
public class EntregaMeteorologica
{
    [MaxLength(128)] public string IdAlerta { get; set; } = "";
    public int IdCidadao { get; set; }
    public DateTime ReservadaEm { get; set; }
    public DateTime? EnviadaEm { get; set; }
    [MaxLength(40)] public string Estado { get; set; } = "Reservada";
}
