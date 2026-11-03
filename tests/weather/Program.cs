using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SantosConecta.Infrastructure.Services;

var now=new DateTime(2026,11,2,12,0,0,DateTimeKind.Utc);
DadosClima Fixture(double? rain,double? gust,int hours=1) {
    using var json=JsonDocument.Parse(JsonSerializer.Serialize(new {hourly=new {time=new[]{new DateTimeOffset(now.AddHours(hours)).ToUnixTimeSeconds()},precipitation=new[]{rain},wind_gusts_10m=new[]{gust}}}));
    return new("Fixture identificada para teste",now,now.AddMinutes(15),json.RootElement.Clone());
}
void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("OK: "+message);}
Check(RegrasMeteorologicas.Avaliar(Fixture(19.9,59.9),now).Count==0,"Abaixo dos limiares não gera aviso");
var alerts=RegrasMeteorologicas.Avaliar(Fixture(20,60),now);
Check(alerts.Count==2 && alerts.All(a=>a.Origem=="Automatico" && a.Severidade=="Preventivo" && a.Descricao.Contains("não é alerta oficial")),"Limiares e origem preventiva explícita");
Check(RegrasMeteorologicas.Avaliar(Fixture(null,null),now).Count==0,"Dados ausentes não geram avisos");
Check(RegrasMeteorologicas.Avaliar(Fixture(30,80,-1),now).Count==0 && RegrasMeteorologicas.Avaliar(Fixture(30,80,25),now).Count==0,"Previsões fora da janela descartadas");
Check(alerts.Select(a=>a.Id).SequenceEqual(RegrasMeteorologicas.Avaliar(Fixture(20,60),now).Select(a=>a.Id)),"Identidade estável para deduplicação");
using var cache=new MemoryCache(new MemoryCacheOptions());
var fake=new FixtureHandler();
var via=new ViaService(new HttpClient(fake),cache);
var road=await via.ConsultarAsync("Av. Ana Costa",default);
await via.ConsultarAsync("Avenida Ana Costa",default);
Check(fake.Calls==1 && road.Segmentos.Count==2 && road.Segmentos[0].Count==3,"Geometria preserva curvas, segmentos separados e cache");
try {await via.ConsultarAsync("x\"];out;",default);throw new Exception("Injeção aceita");} catch(ArgumentException){Console.WriteLine("OK: Nome inválido rejeitado antes da consulta");}
var disabled=new FcmMeteorologia(new HttpClient(fake),new ConfigurationBuilder().Build());
Check(!await disabled.EnviarAsync(alerts[0],"fixture-nao-real",default) && fake.Calls==1,"Push desativado não faz chamada externa");
using var fallbackCache=new MemoryCache(new MemoryCacheOptions());
var fallback=new FixtureHandler { Failures=1 };
var fallbackRoad=await new ViaService(new HttpClient(fallback),fallbackCache).ConsultarAsync("Avenida Ana Costa",default);
Check(fallback.Calls==2 && fallbackRoad.Segmentos.Count==2,"Erro 502 na primeira fonte consulta instância alternativa sem inventar pontos");
using var failureCache=new MemoryCache(new MemoryCacheOptions());
var unavailable=new FixtureHandler { Failures=10 };
var unavailableService=new ViaService(new HttpClient(unavailable),failureCache);
for(var attempt=0;attempt<2;attempt++) {
    try {await unavailableService.ConsultarAsync("Avenida Ana Costa",default);throw new Exception("Falha mascarada como sucesso");}
    catch(HttpRequestException) { }
}
Check(unavailable.Calls==2,"Falha das duas fontes aplica cooldown e não repete chamadas externas");
if(args.Contains("--live")) {
    using var live=new HttpClient{Timeout=TimeSpan.FromSeconds(50)};
    var weather=await new ClimaService(live,cache).ConsultarAsync(default);
    Check(weather.Dados.GetProperty("hourly").GetProperty("time").GetArrayLength()>=24,"Open-Meteo real retorna previsão horária");
    using var liveCache=new MemoryCache(new MemoryCacheOptions());
    var realRoad=await new ViaService(live,liveCache).ConsultarAsync("Avenida Ana Costa",default);
    Check(realRoad.Segmentos.Sum(s=>s.Count)>2,"Ana Costa real tem geometria detalhada");
    Console.WriteLine($"Ana Costa: {realRoad.Segmentos.Count} segmentos, {realRoad.Segmentos.Sum(s=>s.Count)} pontos reais.");
}
Console.WriteLine("Verificações concluídas; nenhuma notificação enviada.");
class FixtureHandler:HttpMessageHandler {
    public int Calls;
    public int Failures;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) {
        Calls++;
        if(request.Method!=HttpMethod.Get) throw new Exception("Geometria deve usar GET.");
        if(Calls<=Failures) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway));
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("""
        {"elements":[{"geometry":[{"lat":-23.95,"lon":-46.33},{"lat":-23.951,"lon":-46.331},{"lat":-23.952,"lon":-46.331}]},{"geometry":[{"lat":-23.953,"lon":-46.332},{"lat":-23.954,"lon":-46.332}]}]}
        """)});
    }
}
