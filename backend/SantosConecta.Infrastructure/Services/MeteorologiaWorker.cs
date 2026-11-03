using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SantosConecta.Domain.Entities;
using SantosConecta.Infrastructure.Data;

namespace SantosConecta.Infrastructure.Services;

// Limiares de triagem preventiva do projeto; não são níveis oficiais de risco.
public static class RegrasMeteorologicas
{
    public static List<AlertaMeteorologico> Avaliar(DadosClima clima, DateTime now)
    {
        var result = new List<AlertaMeteorologico>();
        var h = clima.Dados.GetProperty("hourly");
        var times = h.GetProperty("time");
        for (var i = 0; i < times.GetArrayLength(); i++)
        {
            var start = DateTimeOffset.FromUnixTimeSeconds(times[i].GetInt64()).UtcDateTime;
            if (start < now || start > now.AddHours(24)) continue;
            var rain = h.GetProperty("precipitation")[i];
            var gust = h.GetProperty("wind_gusts_10m")[i];
            void Add(string type, string title, string description, DateTime? intervalStart = null) => result.Add(new AlertaMeteorologico {
                Id = "auto-v1-" + type + "-" + start.ToString("yyyyMMddHH", CultureInfo.InvariantCulture),
                Titulo = title, Descricao = description + " Aviso preventivo automático; não é alerta oficial nem confirmação de alagamento. Acompanhe os órgãos oficiais.",
                PublicadoEm = now, Inicio = intervalStart ?? start, Fim = (intervalStart ?? start).AddHours(1)
            });
            if (rain.ValueKind == JsonValueKind.Number && rain.GetDouble() >= 20)
                // Open-Meteo informa precipitação acumulada na hora anterior ao timestamp.
                Add("chuva", "Previsão de chuva intensa", $"O modelo prevê {rain.GetDouble():0.#} mm em uma hora no ponto consultado em Santos.", start.AddHours(-1));
            if (gust.ValueKind == JsonValueKind.Number && gust.GetDouble() >= 60)
                Add("vento", "Previsão de rajadas fortes", $"O modelo prevê rajadas de {gust.GetDouble():0.#} km/h no ponto consultado em Santos.");
        }
        return result;
    }
}

public class FcmMeteorologia(HttpClient http, IConfiguration config)
{
    private static string B64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
    public async Task<bool> EnviarAsync(AlertaMeteorologico alert, string token, CancellationToken ct)
    {
        // Credencial exclusivamente no servidor. Sem tópicos públicos ou notificações de teste.
        var path = config["Weather:FirebaseServiceAccountPath"];
        if (!config.GetValue("Weather:PushEnabled", false) || string.IsNullOrWhiteSpace(path)) return false;
        using var account = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
        var a = account.RootElement;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = B64(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
        var payload = B64(JsonSerializer.SerializeToUtf8Bytes(new {iss=a.GetProperty("client_email").GetString(),scope="https://www.googleapis.com/auth/firebase.messaging",aud="https://oauth2.googleapis.com/token",iat=now,exp=now+3600}));
        using var rsa = RSA.Create();
        rsa.ImportFromPem(a.GetProperty("private_key").GetString()!);
        var unsigned = header + "." + payload;
        var jwt = unsigned + "." + B64(rsa.SignData(Encoding.UTF8.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        using var auth = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string,string>{{"grant_type","urn:ietf:params:oauth:grant-type:jwt-bearer"},{"assertion",jwt}}), ct);
        auth.EnsureSuccessStatusCode();
        using var authJson = JsonDocument.Parse(await auth.Content.ReadAsStringAsync(ct));
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/v1/projects/" + Uri.EscapeDataString(a.GetProperty("project_id").GetString()!) + "/messages:send");
        request.Headers.Authorization = new("Bearer", authJson.RootElement.GetProperty("access_token").GetString());
        request.Content = JsonContent.Create(new {message=new {token,notification=new {title="Aviso preventivo automático · Santos",body=alert.Titulo},data=new {alertId=alert.Id},android=new {ttl="3600s",collapse_key=alert.Id},apns=new {headers=new Dictionary<string,string>{{"apns-collapse-id",alert.Id},{"apns-expiration",new DateTimeOffset(alert.Fim,TimeSpan.Zero).ToUnixTimeSeconds().ToString()}}}}});
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return true;
    }
}

public class MeteorologiaWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<MeteorologiaWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (config.GetValue("Weather:WorkerEnabled", false))
                {
                    using var scope = scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<SantosConectaDbContext>();
                    var clima = await scope.ServiceProvider.GetRequiredService<ClimaService>().ConsultarAsync(stoppingToken);
                    var now = DateTime.UtcNow;
                    var candidates = RegrasMeteorologicas.Avaliar(clima, now);
                    var existing = await db.AlertasMeteorologicos.Where(a => a.Ativo).ToListAsync(stoppingToken);
                    foreach (var a in existing.Where(a => a.Fim <= now || (a.Origem == "Automatico" && a.Inicio > now && !candidates.Any(c => c.Id == a.Id)))) a.Ativo = false;
                    foreach (var a in candidates)
                    {
                        var stored = await db.AlertasMeteorologicos.FindAsync([a.Id], stoppingToken);
                        if (stored is null) db.AlertasMeteorologicos.Add(a);
                        else { stored.Descricao = a.Descricao; stored.Ativo = true; }
                    }
                    await db.SaveChangesAsync(stoppingToken);
                    if (config.GetValue("Weather:PushEnabled", false))
                    {
                        var alerts = await db.AlertasMeteorologicos.Where(a => a.Ativo && a.Fim > now).ToListAsync(stoppingToken);
                        var preferences = await db.PreferenciasMeteorologicas.Where(p => p.Habilitada && p.Token != null).ToListAsync(stoppingToken);
                        foreach (var a in alerts) foreach (var p in preferences)
                        {
                            if (a.Fim <= DateTime.UtcNow) continue;
                            var permission = await db.PreferenciasMeteorologicas.AsNoTracking().FirstOrDefaultAsync(x=>x.IdCidadao==p.IdCidadao,stoppingToken);
                            if (permission?.Habilitada != true || string.IsNullOrWhiteSpace(permission.Token)) continue;
                            if (await db.EntregasMeteorologicas.AnyAsync(e => e.IdAlerta == a.Id && e.IdCidadao == p.IdCidadao, stoppingToken)) continue;
                            var delivery = new EntregaMeteorologica {IdAlerta=a.Id,IdCidadao=p.IdCidadao,ReservadaEm=DateTime.UtcNow};
                            db.EntregasMeteorologicas.Add(delivery);
                            // Reserva durável antes do envio: sem repetição após resultado de rede ambíguo.
                            await db.SaveChangesAsync(stoppingToken);
                            try
                            {
                                if (await scope.ServiceProvider.GetRequiredService<FcmMeteorologia>().EnviarAsync(a,permission.Token,stoppingToken)) {delivery.EnviadaEm=DateTime.UtcNow;delivery.Estado="Enviada";}
                                else delivery.Estado="Desabilitada";
                            }
                            catch (Exception) when (!stoppingToken.IsCancellationRequested) { delivery.Estado="FalhaOuIncerta"; }
                            await db.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested) { logger.LogWarning("Atualização meteorológica indisponível ({Tipo}).", ex.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }
}
