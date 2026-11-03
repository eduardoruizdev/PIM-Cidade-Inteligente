using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace SantosConecta.Infrastructure.Services;

public record DadosClima(string Fonte, DateTime UltimaAtualizacao, DateTime ValidoAte, JsonElement Dados);
public class ClimaService(HttpClient http, IMemoryCache cache)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    public async Task<DadosClima> ConsultarAsync(CancellationToken ct)
    {
        await Gate.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue<DadosClima>("clima-santos", out var cached)) return cached!;
            const string url = "https://api.open-meteo.com/v1/forecast?latitude=-23.9608&longitude=-46.3336&timezone=America%2FSao_Paulo&timeformat=unixtime&forecast_days=7&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m&hourly=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m,wind_gusts_10m,precipitation_probability,precipitation&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max";
            using var response = await http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var now = DateTime.UtcNow;
            var result = new DadosClima("Open-Meteo · previsão por modelo meteorológico", now, now.AddMinutes(15), json.RootElement.Clone());
            cache.Set("clima-santos", result, TimeSpan.FromMinutes(15));
            return result;
        }
        finally { Gate.Release(); }
    }
}
