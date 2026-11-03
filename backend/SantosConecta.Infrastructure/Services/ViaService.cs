using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace SantosConecta.Infrastructure.Services;

public record GeometriaVia(string Nome, string Fonte, List<List<PontoTransito>> Segmentos);
public class ViaService(HttpClient http, IMemoryCache cache)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    public async Task<GeometriaVia> ConsultarAsync(string nome, CancellationToken ct)
    {
        nome = nome.Trim();
        if (nome.Length is < 3 or > 100 || nome.Any(c => !char.IsLetterOrDigit(c) && c != ' ' && c != '.' && c != '-'))
            throw new ArgumentException("Informe um nome de via válido.");
        if (nome.StartsWith("Av. ", StringComparison.OrdinalIgnoreCase)) nome = "Avenida " + nome[4..];
        var key = "via:" + nome.ToUpperInvariant();
        await Gate.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue<GeometriaVia>(key, out var cached)) return cached!;
            // Área administrativa IBGE de Santos; todos os ways homônimos dentro dela.
            if (cache.Get<bool>("via-falha:" + key)) throw new HttpRequestException("Serviço de geometria temporariamente indisponível. Aguarde um minuto.");
            // Aspas e barras são rejeitadas na validação; preservar acentos no literal QL.
            var query = "[out:json][timeout:10];area[\"boundary\"=\"administrative\"][\"ref:IBGE\"=\"3548500\"]->.s;way(area.s)[\"highway\"][\"name\"=\"" + nome + "\"];out geom;";
            using var json = await ObterAsync(query, key, ct);
            var segments = json.RootElement.GetProperty("elements").EnumerateArray()
                .Where(e => e.TryGetProperty("geometry", out _))
                .Select(e => e.GetProperty("geometry").EnumerateArray().Select(p => new PontoTransito(p.GetProperty("lat").GetDouble(), p.GetProperty("lon").GetDouble())).ToList())
                .Where(p => p.Count >= 2).ToList();
            if (segments.Count == 0) throw new InvalidOperationException("Geometria não disponível para esta via.");
            var result = new GeometriaVia(nome, "© OpenStreetMap contributors · Overpass (ODbL)", segments);
            cache.Set(key, result, TimeSpan.FromHours(24));
            return result;
        }
        finally { Gate.Release(); }
    }

    private async Task<JsonDocument> ObterAsync(string query, string key, CancellationToken ct)
    {
        // Instâncias independentes; GET evita falhas de proxies que rejeitam POST.
        foreach (var endpoint in new[] { "https://overpass-api.de/api/interpreter", "https://overpass.private.coffee/api/interpreter" })
        {
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(ct);
            attempt.CancelAfter(TimeSpan.FromSeconds(12));
            try
            {
                using var response = await http.GetAsync(endpoint + "?data=" + Uri.EscapeDataString(query), attempt.Token);
                response.EnsureSuccessStatusCode();
                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(attempt.Token));
                if (json.RootElement.TryGetProperty("remark", out _) || !json.RootElement.TryGetProperty("elements", out _))
                { json.Dispose(); continue; }
                return json;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested && ex is HttpRequestException or JsonException or OperationCanceledException) { }
        }
        cache.Set("via-falha:" + key, true, TimeSpan.FromMinutes(1));
        throw new HttpRequestException("As fontes de geometria estão temporariamente indisponíveis.");
    }
}
