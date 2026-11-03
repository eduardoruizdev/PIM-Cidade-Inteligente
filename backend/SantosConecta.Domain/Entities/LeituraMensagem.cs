namespace SantosConecta.Domain.Entities;

// Leitura individual da equipe. O cidadão continua usando MensagemOcorrencia.DataLeitura.
public class LeituraMensagem
{
    public int IdMensagem { get; set; }
    public int IdFuncionario { get; set; }
    public DateTime DataLeitura { get; set; }
    public MensagemOcorrencia Mensagem { get; set; } = null!;
    public Funcionario Funcionario { get; set; } = null!;
}

public record NotificacaoMensagem(int IdMensagem, int IdOcorrencia, string Protocolo,
    string Categoria, string NomeRemetente, string Conteudo, DateTime DataEnvio, DateTime? DataLeitura);
