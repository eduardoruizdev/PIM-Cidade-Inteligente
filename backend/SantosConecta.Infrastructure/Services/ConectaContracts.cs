using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace SantosConecta.Infrastructure.Services;

public record AcaoConecta(string Id, string Rotulo, int? Ocorrencia = null, string? Via = null);
public record MensagemConecta(string Papel, string Texto, DateTime Data, string Modo, AcaoConecta[] Acoes);
public record PlanoConecta(string Intencao, string? Protocolo = null, string? Categoria = null, string? Via = null, int Dia = 0, bool Abertas = false);
public record OcorrenciaConecta(int Id, string Protocolo, string Categoria, string Status, DateTime Data);
public record RespostaEquipeConecta(int Quantidade, DateTime? Ultima);
public interface IConectaConsultas
{
    Task<List<OcorrenciaConecta>> MinhasAsync(int cidadao, CancellationToken ct);
    Task<RespostaEquipeConecta> RespostasAsync(int cidadao, int ocorrencia, CancellationToken ct);
    Task<DadosClima> ClimaAsync(CancellationToken ct);
    Task<EstadoTransito> TransitoAsync(string regiao, CancellationToken ct);
    Task<List<SantosConecta.Domain.Entities.AlertaMeteorologico>> AlertasAsync(CancellationToken ct);
}
public interface IConectaModelo
{
    Task<PlanoConecta?> InterpretarAsync(int cidadao, string mensagem, MensagemConecta[] contexto, CancellationToken ct);
}

public static class IntencoesConecta
{
    public static readonly string[] Permitidas = ["nova", "reclamacoes", "respostas", "transito", "clima", "alertas", "mensagens", "ajuda", "fora"];
    public static string Normalizar(string texto) => string.Concat(texto.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)).ToLowerInvariant();
    public static PlanoConecta Local(string texto, MensagemConecta[] contexto)
    {
        var t = Normalizar(texto);
        if (Regex.IsMatch(t, @"\b(senha|password|bearer|api[_ -]?key|token)\b"))
            return new("fora");
        if (Regex.IsMatch(t, @"\b(ignore|ignorar|sql|permissoes|outro cidadao|outra pessoa)\b")) return new("fora");
        var protocol = Regex.Match(texto, @"\bprotocolo\s*(?:n[ºo°.]?\s*)?[#:]?\s*([\w-]{1,40})", RegexOptions.IgnoreCase);
        string? p = protocol.Success && !new[]{"eu","de","da","do","um","uma","minha","esse","este","isso"}.Contains(Normalizar(protocol.Groups[1].Value)) ? protocol.Groups[1].Value : null;
        if(p==null && (t.Contains("esse protocolo") || t.Contains("essa reclamacao"))) {
            p=contexto.Where(m=>m.Papel=="user").Reverse().Select(m=>Local(m.Texto,[]).Protocolo).FirstOrDefault(value=>value!=null);
        }
        var day = t.Contains("amanha") ? 1 : t.Contains("depois de amanha") ? 2 : 0;
        if(t.Contains("depois de amanha")) day=2;
        var via = t.Contains("ana costa") ? "Avenida Ana Costa" : t.Contains("nebias") ? "Avenida Conselheiro Nébias" : null;
        if(t.Contains("alerta") || t.Contains("defesa civil") || t.Contains("aviso de chuva")) return new("alertas");
        if(t.Contains("transito") || t.Contains("congestion") || t.Contains("mapa") || via!=null) return new("transito", Via:via);
        if(t.Contains("chov") || t.Contains("chuva") || t.Contains("temperatura") || t.Contains("clima") || t.Contains("tempo em santos")) return new("clima",Dia:day);
        if(t.Contains("mensage")) return new("mensagens");
        if(t.Contains("respond") || t.Contains("resposta")) return new("respostas",p,Categoria:Categoria(t));
        if(p!=null || t.Contains("protocolo") || t.Contains("acompanha") || t.Contains("status") || t.Contains("minhas reclama") || t.Contains("reclamacoes") || t.Contains("tenho aberta") || t.Contains("reclamacoes eu tenho")) return new("reclamacoes",p,Categoria:Categoria(t),Abertas:t.Contains("aberta"));
        if(t.Contains("reclamar") || t.Contains("registrar") || t.Contains("fazer uma reclama") || t.Contains("buraco") || t.Contains("iluminacao") || t.Contains("lixo")) return new("nova");
        if(t.Length<80 && (t.Contains("amanha") || t.Contains("hoje") || t.Contains("e agora")))
        {
            var previous=contexto.Where(m=>m.Papel=="user").Reverse().Select(m=>Local(m.Texto,[])).FirstOrDefault(plan=>plan.Intencao=="clima");
            if(previous!=null)return previous with {Dia=day};
        }
        return new(t.Contains("ola") || t.Contains("ajud") || t.Contains("obrigad") || t=="oi" ? "ajuda" : "fora");
    }
    private static string? Categoria(string t) => t.Contains("ilumin") ? "iluminacao" : t.Contains("buraco") ? "buracos" : t.Contains("lixo") ? "lixo" : null;
}

// Memória temporária isolada por identidade autenticada; não adiciona tabelas ao SQL.
public sealed class ConectaHistorico : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions {SizeLimit=1000});
    public sealed class Conversa
    {
        public readonly SemaphoreSlim Gate = new(1,1);
        public readonly List<MensagemConecta> Mensagens = [];
    }
    public Conversa Obter(int cidadao)
    {
        lock(cache)
        {
            if(cache.TryGetValue<Conversa>(cidadao,out var c))return c!;
            c=new();cache.Set(cidadao,c,new MemoryCacheEntryOptions{Size=1,AbsoluteExpirationRelativeToNow=TimeSpan.FromHours(24),SlidingExpiration=TimeSpan.FromMinutes(30)});return c;
        }
    }
    public void Dispose()=>cache.Dispose();
}
