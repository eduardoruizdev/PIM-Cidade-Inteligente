using System.ComponentModel.DataAnnotations;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
namespace SantosConecta.Application.Services;

public class NovaMensagemDto { [Required, StringLength(4000, MinimumLength = 1)] public string Conteudo { get; set; } = ""; }
public record MensagemDto(int IdMensagem, int IdOcorrencia, string TipoRemetente, string NomeRemetente, string Conteudo, DateTime DataEnvio, DateTime? DataLeitura);
public class MensagemService(IMensagemRepository repo)
{
    public async Task<object> NotificacoesAsync(int usuario, string tipo, bool administrador) => (await repo.NotificacoesAsync(usuario, tipo, administrador)).Select(m => m with { DataEnvio = DateTime.SpecifyKind(m.DataEnvio, DateTimeKind.Utc), DataLeitura = m.DataLeitura.HasValue ? DateTime.SpecifyKind(m.DataLeitura.Value, DateTimeKind.Utc) : null }).ToList();
    public Task<int> QuantidadeAsync(int usuario, string tipo, bool administrador) => repo.QuantidadeAsync(usuario, tipo, administrador);
    public Task<bool> MarcarNotificacaoAsync(int mensagem, int usuario, string tipo, bool administrador) => repo.MarcarNotificacaoAsync(mensagem, usuario, tipo, administrador);
    public Task<bool> PodeAcessarAsync(int id, int? cidadao) => repo.PodeAcessarAsync(id, cidadao);
    public async Task<List<MensagemDto>> ListarAsync(int id, int usuario, string tipo) => (await repo.ListarAsync(id, usuario, tipo)).Select(m => new MensagemDto(m.IdMensagem, m.IdOcorrencia, m.TipoRemetente, m.NomeRemetente, m.Conteudo, DateTime.SpecifyKind(m.DataEnvio, DateTimeKind.Utc), m.DataLeitura.HasValue ? DateTime.SpecifyKind(m.DataLeitura.Value, DateTimeKind.Utc) : null)).ToList();
    public async Task EnviarAsync(int id, int autor, string tipo, string nome, string conteudo)
    {
        conteudo = conteudo.Trim(); if (conteudo.Length == 0) throw new ArgumentException("Escreva uma mensagem.");
        if (conteudo.Contains('<') || conteudo.Contains('>')) throw new ArgumentException("Envie somente texto, sem marcação HTML.");
        await repo.AdicionarAsync(new MensagemOcorrencia { IdOcorrencia = id, IdRemetente = autor, TipoRemetente = tipo, NomeRemetente = nome, Conteudo = conteudo, DataEnvio = DateTime.UtcNow });
    }
    public Task LidasAsync(int id, string tipo, int usuario, bool administrador) => repo.MarcarLidasAsync(id, tipo, usuario, administrador);
}
