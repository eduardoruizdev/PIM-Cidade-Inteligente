using System.Globalization;
using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
namespace SantosConecta.Infrastructure.Services;

public record RegiaoTransito(string Nome, double Latitude, double Longitude);
public record PontoTransito(double Latitude, double Longitude);
public record TrechoTransito(string Regiao, int VelocidadeAtual, int VelocidadeLivre, bool Interditado, List<PontoTransito> Coordenadas)
{
    public string Nivel => Interditado || VelocidadeLivre <= 0 ? "desconhecido" :
        VelocidadeAtual / (double)VelocidadeLivre >= .75 ? "livre" :
        VelocidadeAtual / (double)VelocidadeLivre >= .35 ? "moderado" : "intenso";
}
public record IncidenteTransito(string Id, string Descricao, string De, string Ate, bool Interditado, List<PontoTransito> Coordenadas);
public record EstadoTransito(bool Disponivel, string Provedor, string Mensagem, DateTime? UltimaAtualizacao, bool IncidentesDisponiveis, RegiaoTransito[] Regioes, List<TrechoTransito> Trechos)
{
    public List<IncidenteTransito> Incidentes { get; init; } = [];
    public DateTime? ValidoAte { get; init; }
    public string MensagemIncidentes { get; init; } = "Incidentes indisponíveis.";
    public bool CamadaMapaDisponivel { get; init; }
    public int TrechosTranquilos => Trechos.Count(t => t.Nivel == "livre");
    public int TrechosModerados => Trechos.Count(t => t.Nivel == "moderado");
    public int TrechosCongestionados => Trechos.Count(t => t.Nivel == "intenso");
    public int TrechosSemInformacao => Trechos.Count(t => t.Nivel == "desconhecido");
    public string SituacaoGeral => !Disponivel ? "indisponivel" : TrechosCongestionados > 0 ? "congestionado" : TrechosModerados > 0 ? "moderado" : TrechosTranquilos > 0 ? "livre" : "indisponivel";
}
public class TransitoService(HttpClient http, IConfiguration config, IMemoryCache cache)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly ConcurrentDictionary<string, int> TileRequests = new();
    // São pontos de consulta, não declarações de cobertura municipal integral. O provedor
    // retorna o trecho mapeado mais próximo de cada ponto; a camada raster cobre só os tiles disponíveis.
    public static readonly RegiaoTransito[] Regioes = [
        new("Orla", -23.972, -46.327), new("Centro", -23.936, -46.329), new("Zona Noroeste", -23.941, -46.371),
        new("Ponta da Praia", -23.985, -46.301), new("Área Continental", -23.872, -46.174),
        new("Av. Ana Costa", -23.966537, -46.332837), new("Av. Conselheiro Nébias", -23.955, -46.326)
    ];
    private async Task<JsonDocument?> ConsultarJsonAsync(string endpoint, string key, CancellationToken ct)
    {
        // Não armazenar respostas de trânsito: respeita inclusive Cache-Control: max-age=0.
        var budgetKey = "traffic-budget:" + DateTime.UtcNow.ToString("yyyyMM", CultureInfo.InvariantCulture);
        var used = cache.Get<int>(budgetKey);
        if (used >= Math.Clamp(config.GetValue("Traffic:MaxRequestsPerMonth", 2000), 0, 20000)) return null;
        cache.Set(budgetKey, used + 1, TimeSpan.FromDays(32));
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.tomtom.com/traffic/services/" + endpoint + "&key=" + Uri.EscapeDataString(key));
        using var response = await http.SendAsync(req, ct);
        if (!response.IsSuccessStatusCode) return null;
        var text = await response.Content.ReadAsStringAsync(ct);
        return JsonDocument.Parse(text);
    }
    public bool CamadaMapaHabilitada => config.GetValue("Traffic:Enabled", false) &&
        !string.IsNullOrWhiteSpace(config["TOMTOM_API_KEY"]);

    public async Task<byte[]?> ConsultarTileFluxoAsync(int zoom, int x, int y, CancellationToken ct)
    {
        var key = config["TOMTOM_API_KEY"];
        if (!CamadaMapaHabilitada || string.IsNullOrWhiteSpace(key)) return null;
        var limit = Math.Clamp(config.GetValue("Traffic:MaxTileRequestsPerMonth", 10000), 0, 1000000);
        var month = DateTime.UtcNow.ToString("yyyyMM", CultureInfo.InvariantCulture);
        if (limit == 0 || TileRequests.AddOrUpdate(month, 1, (_, count) => count + 1) > limit) return null;
        using var req = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.tomtom.com/traffic/map/4/tile/flow/relative0/{zoom}/{x}/{y}.png?key={Uri.EscapeDataString(key)}");
        using var response = await http.SendAsync(req, ct);
        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType != "image/png") return null;
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<EstadoTransito> ConsultarAsync(string? regiao, CancellationToken ct)
    {
        var r = Regioes.FirstOrDefault(r => r.Nome == (regiao ?? "Orla")) ?? throw new ArgumentException("Região inválida.");
        var key = config["TOMTOM_API_KEY"];
        EstadoTransito Indisponivel(string mensagem = "Dados de trânsito indisponíveis. Tente novamente em instantes.") => new(false, "TomTom", mensagem, null, false, Regioes, []) { CamadaMapaDisponivel = CamadaMapaHabilitada };
        if (!config.GetValue("Traffic:Enabled", false) || string.IsNullOrWhiteSpace(key)) return Indisponivel("Dados de trânsito indisponíveis. A integração depende de configuração do serviço.");
        await Gate.WaitAsync(ct);
        try
        {
            // Evita cliques excessivos sem reutilizar dados que o provedor proíbe armazenar.
            var cooldown = "traffic-cooldown:" + r.Nome;
            if (cache.Get<bool>(cooldown)) return Indisponivel("Aguarde alguns segundos antes de consultar esta região novamente.");
            cache.Set(cooldown, true, TimeSpan.FromSeconds(10));
            var point = r.Latitude.ToString(CultureInfo.InvariantCulture) + "," + r.Longitude.ToString(CultureInfo.InvariantCulture);
            List<TrechoTransito> segments = [];
            List<IncidenteTransito> incidents = [];
            bool incidentsAvailable = false;
            var updated = DateTime.UtcNow;
            try
            {
                using var json = await ConsultarJsonAsync("4/flowSegmentData/relative0/14/json?point=" + point + "&unit=KMPH", key, ct);
                if (json is not null)
                {
                    var flow = json.RootElement.GetProperty("flowSegmentData");
                    var points = flow.GetProperty("coordinates").GetProperty("coordinate").EnumerateArray().Select(p => new PontoTransito(p.GetProperty("latitude").GetDouble(), p.GetProperty("longitude").GetDouble())).ToList();
                    if (points.Count >= 2) segments.Add(new(r.Nome, flow.GetProperty("currentSpeed").GetInt32(), flow.GetProperty("freeFlowSpeed").GetInt32(), flow.GetProperty("roadClosure").GetBoolean(), points));
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or KeyNotFoundException or InvalidOperationException) { if (ct.IsCancellationRequested) throw; }
            if (config.GetValue("Traffic:IncidentsEnabled", true))
            {
                try
                {
                    // Recorte local da região; não é um polígono oficial do município.
                    var bbox = string.Join(",", new[] { r.Longitude - .015, r.Latitude - .012, r.Longitude + .015, r.Latitude + .012 }.Select(v => v.ToString(CultureInfo.InvariantCulture)));
                    var fields = Uri.EscapeDataString("{incidents{geometry{type,coordinates},properties{id,iconCategory,events{description},from,to}}}");
                    using var json = await ConsultarJsonAsync("5/incidentDetails?bbox=" + bbox + "&fields=" + fields + "&language=pt-PT&timeValidityFilter=present", key, ct);
                    if (json is not null)
                    {
                        foreach (var incident in json.RootElement.GetProperty("incidents").EnumerateArray())
                        {
                            var p = incident.GetProperty("properties");
                            var g = incident.GetProperty("geometry");
                            List<PontoTransito> points = [];
                            if (g.GetProperty("type").GetString() == "LineString") points = g.GetProperty("coordinates").EnumerateArray().Select(v => new PontoTransito(v[1].GetDouble(), v[0].GetDouble())).ToList();
                            if (g.GetProperty("type").GetString() == "Point") { var v = g.GetProperty("coordinates"); points.Add(new(v[1].GetDouble(), v[0].GetDouble())); }
                            incidents.Add(new(p.GetProperty("id").GetString() ?? "", string.Join(" · ", p.GetProperty("events").EnumerateArray().Select(e => e.GetProperty("description").GetString())), p.TryGetProperty("from", out var from) ? from.GetString() ?? "" : "", p.TryGetProperty("to", out var to) ? to.GetString() ?? "" : "", p.GetProperty("iconCategory").GetInt32() == 8, points));
                        }
                        incidentsAvailable = true;
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or KeyNotFoundException or InvalidOperationException) { if (ct.IsCancellationRequested) throw; }
            }
            if (segments.Count == 0 && !incidentsAvailable) return Indisponivel();
            return new(segments.Count > 0, "TomTom", segments.Count > 0 ? "Trecho próximo à referência selecionada. As cores não cobrem todas as vias de Santos." : "Fluxo indisponível nesta região. Consulte os incidentes disponíveis.", updated, incidentsAvailable, Regioes, segments)
            {
                Incidentes = incidents,
                ValidoAte = updated.AddSeconds(120),
                CamadaMapaDisponivel = CamadaMapaHabilitada,
                MensagemIncidentes = incidentsAvailable ? (incidents.Count == 0 ? "Nenhum incidente informado pelo provedor no recorte consultado." : "Incidentes no entorno da região. O recorte pode incluir vias de municípios vizinhos.") : "Incidentes indisponíveis. Não é possível afirmar ausência de interdições."
            };
        }
        finally { Gate.Release(); }
    }
}
