using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
namespace SantosConecta.Infrastructure.Services;

public record EnderecoEncontrado(string Endereco, decimal Latitude, decimal Longitude, string Bairro);
public class LocalizacaoService(HttpClient http, IMemoryCache cache)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static DateTime ultimaConsulta = DateTime.MinValue;
    public static readonly string[] Bairros = "Alemoa|Aparecida|Areia Branca|Bagres|Barnabé|Bom Retiro|Boqueirão|Cabuçu-Caetê|Campo Grande|Caneleira|Caruara|Castelo|Centro|Chico de Paula|Chinês|Embaré|Encruzilhada|Estuário|Extremo Setentrional|Gonzaga|Guarapá|Ilhéu Alto|Iriri|Jabaquara|José Menino|Jurubatuba|Macuco|Marapé|Monte Cabrão|Monte Serrat|Morro Cachoeira|Morro Caneleira|Morro Chico de Paula|Morro Embaré|Morro Fontana|Morro Jabaquara|Morro Marapé|Morro Nova Cintra|Morro Pacheco|Morro Penha|Morro Saboó|Morro Santa Maria|Morro Santa Terezinha|Morro São Bento|Nossa Senhora das Neves|Outeirinhos|Parque Estadual da Serra do Mar|Paquetá|Piaçaguera|Piratininga|Pompéia|Ponta da Praia|Porto Alemoa|Porto José Menino|Porto Macuco|Porto Paquetá|Porto Ponta da Praia|Porto Saboó|Porto Valongo|Quilombo|Rádio Clube|Saboó|Santa Maria|São Jorge|São Manoel|Trindade|Vale do Quilombo|Valongo|Vila Belmiro|Vila Haddad|Vila Hayden|Vila Mathias|Vila Nova|Vila Progresso".Split('|').OrderBy(x => x, StringComparer.Create(CultureInfo.GetCultureInfo("pt-BR"), false)).ToArray();
    private static string Normalizar(string s) => new(s.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).Select(char.ToLowerInvariant).ToArray());
    public async Task<List<EnderecoEncontrado>> BuscarAsync(string bairro, string rua, string? numero)
    {
        if (!Bairros.Contains(bairro) || rua.Trim().Length < 3 || rua.Length > 180 || (numero?.Length ?? 0) > 20) throw new ArgumentException("Selecione um bairro de Santos e informe uma rua válida.");
        var key = $"geo:{bairro}:{rua.Trim().ToLowerInvariant()}:{numero}";
        if (cache.TryGetValue(key, out List<EnderecoEncontrado>? hit)) return hit!;
        await Gate.WaitAsync();
        try
        {
            if (cache.TryGetValue(key, out hit)) return hit!;
            var espera = TimeSpan.FromSeconds(1.1) - (DateTime.UtcNow - ultimaConsulta);
            if (espera > TimeSpan.Zero) await Task.Delay(espera);
            ultimaConsulta = DateTime.UtcNow;
            var query = Uri.EscapeDataString($"{numero} {rua}, {bairro}, Santos, São Paulo, Brasil");
            using var response = await http.GetAsync($"search?format=jsonv2&addressdetails=1&limit=5&countrycodes=br&q={query}");
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var results = new List<EnderecoEncontrado>();
            foreach (var item in json.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("address", out var a)) continue;
                string Get(string field) => a.TryGetProperty(field, out var v) ? v.GetString() ?? "" : "";
                var cidade = Get("city"); if (cidade.Length == 0) cidade = Get("town");
                var encontrado = Get("suburb"); if (encontrado.Length == 0) encontrado = Get("neighbourhood"); if (encontrado.Length == 0) encontrado = Get("city_district");
                // Sem evidência da cidade, do bairro e do logradouro, não apresentar como endereço confirmado.
                if (Normalizar(cidade) != "santos" || Normalizar(encontrado) != Normalizar(bairro) || Get("road").Length == 0) continue;
                if (!string.IsNullOrWhiteSpace(numero) && Normalizar(Get("house_number")) != Normalizar(numero.Trim())) continue;
                results.Add(new(item.GetProperty("display_name").GetString()!, decimal.Parse(item.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture), decimal.Parse(item.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture), encontrado));
            }
            cache.Set(key, results, TimeSpan.FromHours(24)); return results;
        }
        finally { Gate.Release(); }
    }
}
