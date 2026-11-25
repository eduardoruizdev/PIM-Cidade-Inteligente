namespace SantosConecta.Domain.Entities;

public class FotoOcorrencia
{
    public int IdFoto { get; set; }
    public int IdOcorrencia { get; set; }
    public string NomeArmazenado { get; set; } = "";
    public string TipoConteudo { get; set; } = "";
    public long TamanhoBytes { get; set; }
    public DateTime DataEnvio { get; set; }
    public Ocorrencia Ocorrencia { get; set; } = null!;
}
