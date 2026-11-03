using Microsoft.EntityFrameworkCore;
using SantosConecta.Domain.Entities;
using SantosConecta.Infrastructure.Data;
namespace SantosConecta.Infrastructure.Services;

public class ConectaConsultas(SantosConectaDbContext db, ClimaService clima, TransitoService transito) : IConectaConsultas
{
    // Mesma fonte dos endpoints do cidadão; identidade é aplicada ANTES da projeção.
    public Task<List<OcorrenciaConecta>> MinhasAsync(int cidadao,CancellationToken ct) => db.Ocorrencias.AsNoTracking().Where(o=>o.IdCidadao==cidadao).OrderByDescending(o=>o.DataAbertura)
        .Select(o=>new OcorrenciaConecta(o.IdOcorrencia,o.Protocolo,o.Categoria.Nome,o.Status,o.DataAbertura)).ToListAsync(ct);
    public async Task<RespostaEquipeConecta> RespostasAsync(int cidadao,int ocorrencia,CancellationToken ct)
    {
        var q=db.Mensagens.AsNoTracking().Where(m=>m.IdOcorrencia==ocorrencia && m.Ocorrencia.IdCidadao==cidadao && m.TipoRemetente=="Equipe");
        return new(await q.CountAsync(ct),await q.Select(m=>(DateTime?)m.DataEnvio).MaxAsync(ct));
    }
    public Task<DadosClima> ClimaAsync(CancellationToken ct)=>clima.ConsultarAsync(ct);
    public Task<EstadoTransito> TransitoAsync(string regiao,CancellationToken ct)=>transito.ConsultarAsync(regiao,ct);
    public Task<List<AlertaMeteorologico>> AlertasAsync(CancellationToken ct)=>db.AlertasMeteorologicos.AsNoTracking().Where(a=>a.Ativo && a.Fim>DateTime.UtcNow).OrderBy(a=>a.Inicio).Take(20).ToListAsync(ct);
}
