using SantosConecta.Domain.Entities;
namespace SantosConecta.Domain.Repositories;

public interface IMensagemRepository
{
    Task<int> PendentesAsync(DateTime? inicio, DateTime? fim, string? status, string? prioridade);
    Task<List<NotificacaoMensagem>> NotificacoesAsync(int usuario, string tipo, bool administrador);
    Task<int> QuantidadeAsync(int usuario, string tipo, bool administrador);
    Task<bool> MarcarNotificacaoAsync(int mensagem, int usuario, string tipo, bool administrador);
    Task<bool> PodeAcessarAsync(int ocorrencia, int? cidadao);
    Task<List<MensagemOcorrencia>> ListarAsync(int ocorrencia, int usuario, string tipo);
    Task AdicionarAsync(MensagemOcorrencia mensagem);
    Task MarcarLidasAsync(int ocorrencia, string tipoLeitor, int usuario, bool administrador);
}
