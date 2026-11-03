using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace SantosConecta.Infrastructure.Services;

public record CategoriaServico(string Id,string Nome);
public record ServicoPublico(string Id,string Nome,string Categoria,string? Endereco,string? Bairro,string? Telefone,string? Horario,double? Latitude,double? Longitude,string Fonte,DateTime ConsultadoEm,DateTime? AtualizacaoFonte=null,double? DistanciaKm=null);
public record ListaServicos(List<ServicoPublico> Itens,int Total,string[] Bairros,DateTime ConsultadoEm,bool Desatualizado,string Fonte);
public class ServicosPublicosService(HttpClient http,IMemoryCache cache)
{
    public const string Fonte="https://www.santos.sp.gov.br/locais";
    private static readonly SemaphoreSlim Gate=new(1,1);
    public static readonly CategoriaServico[] Categorias=[new("ubs","UBS e policlínicas"),new("upa","UPAs e pronto atendimento"),new("hospital","Hospitais públicos"),new("escola","Escolas públicas"),new("assistencia","CRAS e assistência social"),new("municipal","Unidades e serviços municipais"),new("biblioteca","Bibliotecas públicas"),new("cultura-esporte","Centros esportivos e culturais")];
    public static string Normalizar(string value)=>string.Concat(value.Normalize(NormalizationForm.FormD).Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)).ToLowerInvariant();
    private static string? Categoria(string nome)
    {
        var n=Normalizar(nome);
        if(n.Contains("policlinica")||n.StartsWith("ubs ")||n.Contains("unidade basica de saude"))return "ubs";
        if(n.StartsWith("upa ")||n.Contains("pronto atendimento")||n.Contains("pronto-socorro"))return "upa";
        if(n.Contains("hospital")&&(n.Contains("municipal")||n.Contains("estadual")||n.Contains("guilherme alvaro")))return "hospital";
        if(n.StartsWith("ume ")||n.Contains("escola municipal")||n.Contains("escola estadual"))return "escola";
        if(n.Contains("cras")||n.Contains("creas")||n.Contains("centro de referencia")&&n.Contains("assistencia"))return "assistencia";
        if(n.StartsWith("biblioteca ")||n.StartsWith("gibiteca "))return "biblioteca";
        if(n.Contains("centro esportivo")||n.Contains("centro cultural")||n.Contains("teatro municipal")||n=="arena santos"||n.StartsWith("vila criativa"))return "cultura-esporte";
        if(n.Contains("paco municipal")||n.StartsWith("secretaria municipal")||n.StartsWith("prefeitura regional")||n.StartsWith("subprefeitura")||n.StartsWith("procon"))return "municipal";
        return null; // Não classificar estabelecimentos privados como públicos por aproximação.
    }
    public static List<ServicoPublico> Interpretar(string html,DateTime consulta)
    {
        var start=html.IndexOf("[{\"type\":\"local\"",StringComparison.Ordinal);
        if(start<0){html=WebUtility.HtmlDecode(html);start=html.IndexOf("[{\"type\":\"local\"",StringComparison.Ordinal);}
        if(start<0)throw new InvalidOperationException("Formato do catálogo oficial não reconhecido.");
        var reader=new Utf8JsonReader(Encoding.UTF8.GetBytes(html[start..]));
        using var doc=JsonDocument.ParseValue(ref reader);
        var result=new List<ServicoPublico>();
        foreach(var item in doc.RootElement.EnumerateArray())
        {
            string? Field(string name)=>item.TryGetProperty(name,out var v)&&v.ValueKind==JsonValueKind.String&&!string.IsNullOrWhiteSpace(v.GetString())?WebUtility.HtmlDecode(Regex.Replace(v.GetString()!,"<[^>]+>"," ")).Trim():null;
            var nome=Field("title");var id=Field("nid");if(nome==null||id==null||!id.All(char.IsDigit))continue;
            var categoria=Categoria(nome);if(categoria==null)continue;
            var address=Field("field_endereco");string? bairro=null;
            if(address!=null){var parts=Regex.Split(address,@"\s[-–]\s");if(parts.Length>1){var candidate=parts[1].Split(',')[0].Trim();if(candidate.Length is >1 and <70&&!candidate.Any(char.IsDigit)&&!new[]{"santos","sp","brasil","sao paulo"}.Contains(Normalizar(candidate)))bairro=candidate;}}
            double? lat=null,lon=null;
            var point=Field("field_ponto")?.Split(',');
            if(point?.Length==2&&double.TryParse(point[0],NumberStyles.Float,CultureInfo.InvariantCulture,out var a)&&double.TryParse(point[1],NumberStyles.Float,CultureInfo.InvariantCulture,out var b)&&double.IsFinite(a)&&double.IsFinite(b)&&a is >=-90 and <=90&&b is >=-180 and <=180){lat=a;lon=b;}
            result.Add(new("santos-"+id,nome,categoria,address,bairro,Field("field_telefone"),Field("field_horario"),lat,lon,Fonte,consulta));
        }
        if(result.Count==0)throw new InvalidOperationException("Catálogo oficial sem unidades reconhecidas.");
        return result.DistinctBy(s=>s.Id).ToList();
    }
    private async Task<List<ServicoPublico>> Catalogo(CancellationToken ct)
    {
        await Gate.WaitAsync(ct);
        try
        {
            var saved=cache.Get<List<ServicoPublico>>("servicos-oficiais");
            if(saved!=null&&saved[0].ConsultadoEm>DateTime.UtcNow.AddHours(-24))return saved;
            if(cache.Get<bool>("servicos-cooldown")){if(saved!=null)return saved;throw new HttpRequestException("Catálogo temporariamente indisponível.");}
            try
            {
                var html=await http.GetStringAsync(Fonte,ct);
                if(html.Length>4_000_000)throw new InvalidOperationException("Catálogo excede limite.");
                var parsed=Interpretar(html,DateTime.UtcNow);
                cache.Set("servicos-oficiais",parsed,TimeSpan.FromDays(7));return parsed;
            }
            catch(Exception ex) when(!ct.IsCancellationRequested&&ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException)
            {cache.Set("servicos-cooldown",true,TimeSpan.FromMinutes(1));if(saved!=null)return saved;throw new HttpRequestException("Não foi possível consultar o catálogo oficial. Tente novamente.");}
        }
        finally{Gate.Release();}
    }
    public static double Distancia(double lat,double lon,double destinoLat,double destinoLon)
    {const double rad=Math.PI/180;var a=Math.Pow(Math.Sin((destinoLat-lat)*rad/2),2)+Math.Cos(lat*rad)*Math.Cos(destinoLat*rad)*Math.Pow(Math.Sin((destinoLon-lon)*rad/2),2);return 6371*2*Math.Asin(Math.Sqrt(Math.Clamp(a,0,1)));}
    public async Task<ListaServicos> Listar(string? categoria,string? bairro,string? texto,double? latitude,double? longitude,double? raio,int limite,int inicio,CancellationToken ct)
    {
        if(categoria!=null&&!Categorias.Any(c=>c.Id==categoria)||bairro?.Length>100||texto?.Length>100||limite is <1 or >200||inicio is <0 or >5000||latitude.HasValue!=longitude.HasValue||latitude.HasValue&&(!double.IsFinite(latitude.Value)||latitude is <-90 or >90)||longitude.HasValue&&(!double.IsFinite(longitude.Value)||longitude is <-180 or >180)||raio.HasValue&&(!double.IsFinite(raio.Value)||raio is <=0 or >100||!latitude.HasValue))throw new ArgumentException("Filtros inválidos. Use limite de 1 a 200 e raio de até 100 km com coordenadas válidas.");
        var all=await Catalogo(ct);IEnumerable<ServicoPublico> query=all;
        if(!string.IsNullOrWhiteSpace(categoria))query=query.Where(s=>s.Categoria==categoria);
        if(!string.IsNullOrWhiteSpace(bairro))query=query.Where(s=>s.Bairro!=null&&Normalizar(s.Bairro)==Normalizar(bairro));
        if(!string.IsNullOrWhiteSpace(texto))query=query.Where(s=>Normalizar(s.Nome).Contains(Normalizar(texto.Trim())));
        if(latitude.HasValue)query=query.Select(s=>s.Latitude.HasValue&&s.Longitude.HasValue?s with{DistanciaKm=Distancia(latitude.Value,longitude!.Value,s.Latitude.Value,s.Longitude.Value)}:s);
        if(raio.HasValue)query=query.Where(s=>s.DistanciaKm<=raio);
        var filtered=query.OrderBy(s=>s.DistanciaKm??double.MaxValue).ThenBy(s=>s.Nome).ToList();
        return new(filtered.Skip(inicio).Take(limite).ToList(),filtered.Count,all.Where(s=>s.Bairro!=null).Select(s=>s.Bairro!).Distinct().Order().ToArray(),all[0].ConsultadoEm,all[0].ConsultadoEm<DateTime.UtcNow.AddHours(-24),Fonte);
    }
    public async Task<ServicoPublico?> Detalhe(string id,CancellationToken ct)=>Regex.IsMatch(id,@"^santos-\d{1,20}$")?(await Catalogo(ct)).FirstOrDefault(s=>s.Id==id):null;
}
