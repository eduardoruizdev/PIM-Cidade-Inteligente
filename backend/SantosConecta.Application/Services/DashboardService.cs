using SantosConecta.Application.DTOs;
using SantosConecta.Domain.Repositories;
namespace SantosConecta.Application.Services;

public class DashboardService(IOcorrenciaRepository ocorrencias)
{
    public async Task<DashboardResumoDto> ResumoAsync(string? status, string? prioridade)
    {
        var dados = await ocorrencias.ListarAsync(status, prioridade);
        return new(dados.Count, dados.Count(o => o.Status == "Aberta" || o.Status == "Em análise"),
            dados.Count(o => o.Status == "Em andamento"), dados.Count(o => o.Status == "Resolvida"));
    }
    public async Task<List<BairroDto>> BairrosAsync(string? status, string? prioridade) =>
        (await ocorrencias.ListarAsync(status, prioridade)).GroupBy(o => o.Bairro)
        .Select(g => new BairroDto(g.Key, g.Count())).OrderByDescending(g => g.Quantidade).ToList();
}
