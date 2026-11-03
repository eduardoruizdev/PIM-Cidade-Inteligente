using Microsoft.AspNetCore.Mvc;
using SantosConecta.Infrastructure.Services;
using SantosConecta.API.Controllers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("OK: " + message); }
var service = new TravessiaService();
var controller = new TravessiasController(service);
var result = (DadosTravessia)((OkObjectResult)await controller.Atual()).Value!;
Check(result.Sentidos.Count == 2 && result.Sentidos.Select(x => x.Sentido).Distinct().Count() == 2, "API contém ambos os sentidos");
Check(result.Sentidos.All(x => !x.Disponivel && x.EsperaMinutos is null && x.InformadoEm is null && x.ConsultadoEm is null), "Ausência de fonte não inventa valores nem horários");
Check(result.Modo == "consulta_oficial" && result.Embarcacoes is null && result.Comunicados.Count == 0, "Fallback explícito sem operação ou comunicados inventados");
Check(ReferenceEquals(service.Consultar(), service.Consultar()), "Consultas repetidas reutilizam resposta sem tráfego externo");
foreach (var period in new[] {"horas", "hoje", "7dias"}) {
 var json = JsonSerializer.SerializeToElement(((OkObjectResult)controller.Historico(period)).Value);
 Check(!json.GetProperty("suficiente").GetBoolean() && json.GetProperty("itens").GetArrayLength() == 0, "Histórico vazio honesto: " + period);
}
try { service.Historico("invalido"); throw new Exception("Aceitou período inválido"); }
catch (ArgumentException) { Console.WriteLine("OK: período inválido rejeitado"); }
Check(typeof(TravessiasController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true).Length == 1, "Endpoint segue autenticação existente");

var clock = new FixtureClock();
var pages = new PageHandler();
var checkedService = new TravessiaService(new HttpClient(pages), timeProvider: clock);
var checkedResult = await checkedService.ConsultarAsync();
Check(checkedResult.ConsultaExterna is { Disponivel: true, Nome: "METROA · fonte independente" }, "Falha oficial permite página alternativa validada, rotulada como terceiro");
await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => checkedService.ConsultarAsync()));
Check(pages.Calls == 2, "Cache e exclusão mútua evitam novas requisições para dez consultas");
clock.Now = clock.Now.AddMinutes(31);
pages.Offline = true;
checkedResult = await checkedService.ConsultarAsync();
Check(checkedResult.ConsultaExterna is { Disponivel: false, Url: null } && checkedResult.Sentidos.All(s => s.EsperaMinutos is null), "Expiração e fonte offline removem link antigo sem criar medições");
await checkedService.ConsultarAsync();
Check(pages.Calls == 4, "Cache negativo impede insistência em páginas indisponíveis");
pages.Offline = false;
pages.Html = "<html><title>Outra página sem dados</title></html>";
clock.Now = clock.Now.AddMinutes(31);
Check((await checkedService.ConsultarAsync()).ConsultaExterna?.Disponivel == false, "HTTP 200 com conteúdo irrelevante não valida consulta");
pages.Html = "<html>Santos Guaruja Just a moment</html>";
clock.Now = clock.Now.AddMinutes(31);
Check((await checkedService.ConsultarAsync()).ConsultaExterna?.Disponivel == false, "Desafio de acesso não é tratado como página válida");
pages.Redirect = true;
clock.Now = clock.Now.AddMinutes(31);
Check((await checkedService.ConsultarAsync()).ConsultaExterna?.Disponivel == false, "Redirecionamento para host desconhecido não é seguido");
using (var cancelled = new CancellationTokenSource()) {
 cancelled.Cancel();
 try { await checkedService.ConsultarAsync(cancelled.Token); throw new Exception("Cancelamento ignorado"); }
 catch (OperationCanceledException) { Console.WriteLine("OK: cancelamento do cliente respeitado"); }
}
if (args.Contains("--live")) {
 using var liveClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
 liveClient.DefaultRequestHeaders.UserAgent.ParseAdd("SantosConecta/3.0 (verificacao-de-link)");
 using var logFactory = LoggerFactory.Create(b => b.AddConsole());
 var live = await new TravessiaService(liveClient, logFactory.CreateLogger<TravessiaService>()).ConsultarAsync();
 Console.WriteLine("Consulta HTTP real: " + JsonSerializer.Serialize(live.ConsultaExterna));
 Check(live.Sentidos.All(x => x.EsperaMinutos is null), "Verificação de link não se transforma em medição inventada");
}

class FixtureClock : TimeProvider {
 public DateTimeOffset Now = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
 public override DateTimeOffset GetUtcNow() => Now;
}
class PageHandler : HttpMessageHandler {
 public int Calls;
 public bool Offline;
 public bool Redirect;
 public string Html = "<html><title>Santos Guaruja — consulta fixture</title></html>";
 protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
  Calls++;
  if (Redirect) {
   var response = new HttpResponseMessage(System.Net.HttpStatusCode.Redirect);
   response.Headers.Location = new Uri("https://exemplo.invalid/");
   return Task.FromResult(response);
  }
  if (Offline || request.RequestUri!.Host == "semil.sp.gov.br")
   return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
  return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
   Content = new StringContent(Html, System.Text.Encoding.UTF8, "text/html")
  });
 }
}
