using SantosConecta.Application.DTOs;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
namespace SantosConecta.Application.Services;

public class DashboardService(IOcorrenciaRepository ocorrencias, IMensagemRepository mensagens)
{
    private async Task<List<Ocorrencia>> Dados(string? status, string? prioridade, DateTime? inicio, DateTime? fim)
    {
        if (inicio > fim) throw new ArgumentException("O início do período deve ser anterior ao fim.");
        return (await ocorrencias.ListarAsync(status, prioridade)).Where(o => (!inicio.HasValue || o.DataAbertura >= inicio.Value.Date) && (!fim.HasValue || o.DataAbertura < fim.Value.Date.AddDays(1))).ToList();
    }
    public async Task<DashboardResumoDto> ResumoAsync(string? status, string? prioridade, DateTime? inicio = null, DateTime? fim = null)
    {
        var dados = await Dados(status, prioridade, inicio, fim);
        var tempos = dados.Where(o => o.Status == "Resolvida").Select(o => new { o.DataAbertura, Final = o.Historico.Where(h => h.Descricao == "Status alterado para Resolvida.").Select(h => (DateTime?)h.DataAlteracao).Max() }).Where(o => o.Final.HasValue).Select(o => (o.Final!.Value - o.DataAbertura).TotalHours).ToList();
        return new(dados.Count, dados.Count(o => o.Status == "Aberta" || o.Status == "Em análise"), dados.Count(o => o.Status == "Em andamento"), dados.Count(o => o.Status == "Resolvida"), dados.Count(o => o.Status == "Aberta"), dados.Count(o => o.Status == "Em análise"), dados.Count(o => o.Status != "Resolvida" && o.Status != "Cancelada" && o.DataAbertura < DateTime.UtcNow.AddDays(-7)), tempos.Count > 0 ? Math.Round(tempos.Average(), 1) : null, await mensagens.PendentesAsync(inicio?.Date, fim?.Date.AddDays(1), status, prioridade));
    }
    public async Task<List<BairroDto>> BairrosAsync(string? status, string? prioridade, DateTime? inicio = null, DateTime? fim = null) => (await Dados(status, prioridade, inicio, fim)).GroupBy(o => o.Bairro).Select(g => new BairroDto(g.Key, g.Count())).OrderByDescending(g => g.Quantidade).ToList();
}