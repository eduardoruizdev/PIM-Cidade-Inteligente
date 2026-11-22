using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SantosConecta.API.Controllers;
using SantosConecta.Infrastructure.Services;

void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("OK: " + message); }
var clock = new FixtureClock();
using var factory = LoggerFactory.Create(_ => { });
var handler = new PageHandler();
var client = new HttpClient(handler);
var service = new TravessiaService(client, factory.CreateLogger<TravessiaService>(), clock);
var controller = new TravessiasController(service);
var first = await service.ConsultarAsync();
Check(first.Sentidos.Count == 2 && first.Sentidos[0].EsperaMinutos == 15 && first.Sentidos[1].EsperaMinutos == 55,
    "HTML oficial é interpretado para ambos os sentidos");
Check(first.Embarcacoes == 9 && first.Comunicados.Single().Contains("maré forte"),
    "Quantidade de embarcações e aviso operacional vêm do HTML oficial");
Check(first.CapturadoEm == clock.Now && first.Sentidos.All(x => x.InformadoEm == clock.Now),
    "Timestamp identifica consulta à fonte, sem inventar hora original da medição");
Check(handler.Calls == 1 && (await service.ConsultarAsync()).CapturadoEm == first.CapturadoEm && handler.Calls == 1,
    "Cache evita chamada externa repetida dentro do minuto");
clock.Now = clock.Now.AddMinutes(4);
handler.Offline = true;
var stale = await service.ConsultarAsync();
Check(stale.Sentidos.All(x => x.Desatualizado) && stale.Sentidos[0].EsperaMinutos == 15,
    "Indisponibilidade preserva última resposta válida identificada como desatualizada");
var api = (DadosTravessia)((OkObjectResult)await controller.Atual()).Value!;
Check(api.FonteUrl == "https://acquavias.com.br/santos-guaruja/" && api.Sentidos.Count == 2,
    "Endpoint REST existente entrega a fonte oficial e os sentidos");
handler.HtmlData = "<html><div data-travessia-id=\"1951\">sem tempos</div></html>";
clock.Now = clock.Now.AddMinutes(4);
var retained = await service.ConsultarAsync();
Check(retained.Sentidos[0].EsperaMinutos == 15 && retained.Sentidos.All(x => x.Desatualizado),
    "Resposta inválida não substitui cache válido");
var invalid = false;
try { TravessiaService.Parse("<html>outra página</html>", clock.Now); }
catch (InvalidDataException) { invalid = true; }
Check(invalid, "Página incompatível é rejeitada sem gerar dados");
var sourceFileIndex = Array.IndexOf(args, "--html-file");
if (sourceFileIndex >= 0 && sourceFileIndex + 1 < args.Length)
{
    var source = TravessiaService.Parse(File.ReadAllText(args[sourceFileIndex + 1]), DateTimeOffset.UtcNow);
    Check(source.Sentidos.All(x => x.Disponivel && x.EsperaMinutos is >= 0),
        "Parser extrai os dois tempos do HTML oficial baixado agora");
    Console.WriteLine("Leitura oficial interpretada: " + JsonSerializer.Serialize(source));
}
foreach (var period in new[] { "horas", "hoje", "7dias" })
    Check(!JsonSerializer.SerializeToElement(service.Historico(period)).GetProperty("suficiente").GetBoolean(), "Histórico sem leituras não inventa dados: " + period);
Console.WriteLine("Verificações da integração de travessia aprovadas.");
if (args.Contains("--live"))
{
    using var liveHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    var live = await new TravessiaService(liveHttp, factory.CreateLogger<TravessiaService>()).ConsultarAsync();
    Console.WriteLine("Resposta da fonte oficial: " + JsonSerializer.Serialize(live));
    Check(live.Sentidos.Any(x => x.Disponivel && x.EsperaMinutos is not null) || live.Modo == "indisponivel",
        "Consulta live retornou dados parseáveis ou indisponibilidade explícita");
}

class FixtureClock : TimeProvider { public DateTimeOffset Now = new(2026, 11, 22, 15, 0, 0, TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Now; }
class PageHandler : HttpMessageHandler
{
    private const string Html = """
    <html><div class="aqv-aviso-usuario">Travessia com lentidão por motivo de maré forte.</div>
    <a data-travessia-id="1951"><div class="aqv-topbar__superior"><div class="aqv-topbar__nomes"><span>Santos</span><span>Guarujá</span></div>
    <div class="aqv-topbar__tempos"><span>15 min</span><span>55 min</span></div></div>
    <div class="aqv-topbar__embarcacoes" aria-label="9 embarcações em operação"><span>9</span></div></a></html>
    """;
    public int Calls; public bool Offline; public string HtmlData = Html;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Calls++;
        if (Offline) return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
            Content = new StringContent(HtmlData, System.Text.Encoding.UTF8, "text/html")
        });
    }
}
