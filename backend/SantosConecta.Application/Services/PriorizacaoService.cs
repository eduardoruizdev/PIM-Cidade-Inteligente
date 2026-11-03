using SantosConecta.Application.DTOs;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
namespace SantosConecta.Application.Services;

public record PontuacaoDto(int Gravidade,int Tempo,int Vulnerabilidade,int Impacto,int Calculada,int Efetiva,int DiasEspera,bool RevisaoUrgente,string Justificativa);
public static class CalculoPrioridade
{
    public static PontuacaoDto Calcular(Ocorrencia o,IndicadorTerritorial? indicador,DateTime agora)
    {
        var dias=Math.Max(0,(int)(agora-o.DataAbertura).TotalDays);
        var tempo=Math.Min(20,dias/2); // um ponto por dois dias, teto aos 40 dias
        var vulnerabilidade=indicador is null ? 0 : (int)Math.Round(Math.Clamp(indicador.IndiceVulnerabilidade,0,100)*.25m,MidpointRounding.AwayFromZero);
        var gravidade=Math.Clamp(o.Gravidade,0,40); var impacto=Math.Clamp(o.ImpactoColetivo,0,15);
        var total=gravidade+tempo+vulnerabilidade+impacto;
        return new(gravidade,tempo,vulnerabilidade,impacto,total,o.PontuacaoManual??total,dias,o.RiscoImediato,
            $"Gravidade {gravidade}/40 + espera {tempo}/20 + território {vulnerabilidade}/25 + impacto {impacto}/15 = {total}/100. " +
            (indicador is null ? "Sem indicador territorial integrado; componente territorial zero não significa ausência de vulnerabilidade. " : $"{indicador.TipoDado}; fonte: {indicador.Fonte}; ano: {indicador.AnoReferencia}. ")+
            (o.PontuacaoManual.HasValue ? $"Revisão humana: {o.PontuacaoManual}/100. {o.JustificativaPrioridade}" : "Parâmetros acadêmicos, sujeitos à revisão humana."));
    }
}
public class PriorizacaoService(IOcorrenciaRepository ocorrencias,IPortalRepository portal)
{
    public Task<List<IndicadorTerritorial>> IndicadoresAsync()=>portal.IndicadoresAsync();
    public async Task<object> FilaAsync()
    {
        var indicadores=await portal.IndicadoresAsync();
        return (await ocorrencias.ListarAsync()).Where(o=>o.Status!="Resolvida"&&o.Status!="Cancelada")
            .Select(o=>new { o.IdOcorrencia,o.Protocolo,Categoria=o.Categoria.Nome,o.Bairro,o.Status,o.Gravidade,o.ImpactoColetivo,o.RiscoImediato,o.PontuacaoManual,
                Indicador=indicadores.FirstOrDefault(i=>string.Equals(i.Bairro,o.Bairro,StringComparison.OrdinalIgnoreCase)),
                Pontuacao=CalculoPrioridade.Calcular(o,indicadores.FirstOrDefault(i=>string.Equals(i.Bairro,o.Bairro,StringComparison.OrdinalIgnoreCase)),DateTime.UtcNow) })
            .OrderByDescending(x=>x.Pontuacao.RevisaoUrgente).ThenByDescending(x=>x.Pontuacao.Efetiva).ThenByDescending(x=>x.Pontuacao.DiasEspera).ToList();
    }
    public async Task<bool> RevisarAsync(int id,RevisarPrioridadeDto dto,int funcionario)
    {
        var o=await ocorrencias.ObterAsync(id);if(o is null)return false;
        if(string.IsNullOrWhiteSpace(dto.Justificativa)||dto.Justificativa.Trim().Length<10)throw new ArgumentException("Justifique a revisão com pelo menos 10 caracteres.");
        var anterior=$"G{o.Gravidade}, I{o.ImpactoColetivo}, risco {o.RiscoImediato}, manual {o.PontuacaoManual?.ToString()??"não"}";
        o.Gravidade=dto.Gravidade;o.ImpactoColetivo=dto.ImpactoColetivo;o.RiscoImediato=dto.RiscoImediato;o.PontuacaoManual=dto.PontuacaoManual;o.JustificativaPrioridade=dto.Justificativa.Trim();
        o.Historico.Add(new HistoricoOcorrencia {IdFuncionario=funcionario,DataAlteracao=DateTime.UtcNow,Descricao=$"Prioridade: {anterior} → G{o.Gravidade}, I{o.ImpactoColetivo}, risco {o.RiscoImediato}, manual {o.PontuacaoManual?.ToString()??"não"}. {o.JustificativaPrioridade}"});
        await ocorrencias.SalvarAsync();return true;
    }
    public async Task<object> EquidadeAsync()
    {
        var dados=await ocorrencias.ListarAsync();var indicadores=await portal.IndicadoresAsync();
        return new { AntigasNaoResolvidas=dados.Count(o=>o.Status!="Resolvida"&&o.Status!="Cancelada" && o.DataAbertura<DateTime.UtcNow.AddDays(-30)),
            MaiorVulnerabilidade=dados.Count(o=>indicadores.Any(i=>string.Equals(i.Bairro,o.Bairro,StringComparison.OrdinalIgnoreCase)&&i.IndiceVulnerabilidade>=60)),
            PorPrioridade=dados.GroupBy(o=>o.Prioridade).Select(g=>new {Prioridade=g.Key,Quantidade=g.Count()}),
            Regioes=dados.GroupBy(o=>o.Bairro).Select(g=>new {Bairro=g.Key,Quantidade=g.Count(),TempoMedioDias=g.Where(o=>o.Status=="Resolvida").Select(o=>o.Historico.Where(h=>h.Descricao=="Status alterado para Resolvida.").OrderByDescending(h=>h.DataAlteracao).FirstOrDefault()).Where(h=>h!=null).Select(h=>(double?)(h!.DataAlteracao-dados.First(o=>o.IdOcorrencia==h.IdOcorrencia).DataAbertura).TotalDays).DefaultIfEmpty(null).Average()}) };
    }
}
