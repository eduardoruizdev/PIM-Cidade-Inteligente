using System.Net;
using Microsoft.Extensions.Caching.Memory;
using SantosConecta.Infrastructure.Services;
const string fixture="""
<html>[{"type":"local","title":"Policlínica Fixture","nid":"1","field_endereco":"Rua Fixture, 1 - Centro","field_ponto":"-23.96, -46.333","field_telefone":"","bairro":"123"},{"type":"local","title":"UME Fixture","nid":"2","field_endereco":"Rua Fixture, 2 - Aparecida","field_ponto":"-23.98, -46.31"},{"type":"local","title":"Hospital Privado Fixture","nid":"3","field_ponto":"-23.95,-46.33"},{"type":"local","title":"CRAS Fixture","nid":"4","field_ponto":"NaN,0"}]</html>
""";
var count=0;void Check(bool yes,string name){if(!yes)throw new Exception(name);count++;Console.WriteLine("OK: "+name);}
var parsed=ServicosPublicosService.Interpretar(fixture,DateTime.UtcNow);
Check(parsed.Count==3&&!parsed.Any(s=>s.Id=="santos-3"),"Não inclui hospital privado como público");
Check(parsed[0].Bairro=="Centro"&&parsed[0].Telefone==null&&parsed[0].Horario==null,"Bairro vem do endereço; campos ausentes não são inventados");
Check(parsed.Last().Latitude==null,"Coordenadas inválidas não são desenhadas");
using var cache=new MemoryCache(new MemoryCacheOptions());var handler=new Source(fixture);var service=new ServicosPublicosService(new HttpClient(handler),cache);
var all=await service.Listar(null,null,null,null,null,null,100,0,default);
var filtered=await service.Listar("ubs","Centro","policlinica",null,null,null,100,0,default);
Check(filtered.Total==1&&handler.Calls==1,"Filtros sem acentos e categoria reutilizam cache oficial");
var near=await service.Listar(null,null,null,-23.96,-46.333,1,100,0,default);
Check(near.Total==1&&near.Itens[0].DistanciaKm==0,"Filtro de raio usa distância real entre coordenadas");
Check(all.Itens.All(s=>s.DistanciaKm==null),"Coordenadas do cidadão não modificam o catálogo compartilhado");
Check(await service.Detalhe("santos-1",default)!=null&&await service.Detalhe("inexistente",default)==null,"Detalhes por identidade e ausência explícita");
foreach(var invalid in new[]{-1d,101d,double.NaN}){try{await service.Listar(null,null,null,0,0,invalid,100,0,default);throw new Exception("Raio aceito");}catch(ArgumentException){}}
Check(true,"Valida raio, valores não finitos e limites");
try{await service.Listar(null,null,null,0,null,null,100,0,default);throw new Exception("Coordenadas incompletas");}catch(ArgumentException){Check(true,"Recusa coordenadas incompletas");}
try{ServicosPublicosService.Interpretar("<html>layout alterado</html>",DateTime.UtcNow);throw new Exception("Mudança mascarada");}catch(InvalidOperationException){Check(true,"Mudança de layout não produz catálogo fictício");}
using var failureCache=new MemoryCache(new MemoryCacheOptions());var failureSource=new Source("<html>indisponível</html>");var unavailable=new ServicosPublicosService(new HttpClient(failureSource),failureCache);
for(var i=0;i<2;i++){try{await unavailable.Listar(null,null,null,null,null,null,100,0,default);throw new Exception("Falha mascarada");}catch(HttpRequestException){}}
Check(failureSource.Calls==1,"Falha de fonte não gera dados e aplica cooldown");
failureCache.Set("servicos-oficiais",parsed.Select(s=>s with{ConsultadoEm=DateTime.UtcNow.AddDays(-2)}).ToList(),TimeSpan.FromDays(1));
var stale=await unavailable.Listar(null,null,null,null,null,null,100,0,default);
Check(stale.Desatualizado&&stale.ConsultadoEm<DateTime.UtcNow.AddDays(-1),"Cópia antiga é identificada com data original");
Console.WriteLine($"{count} verificações de serviços aprovadas; fixtures identificadas.");
if(args.Contains("--live")){
 using var liveCache=new MemoryCache(new MemoryCacheOptions());using var http=new HttpClient{Timeout=TimeSpan.FromSeconds(25)};
 var live=await new ServicosPublicosService(http,liveCache).Listar(null,null,null,null,null,null,200,0,default);
 Check(live.Total>0,"Catálogo real da Prefeitura interpretado");Console.WriteLine($"Fonte real: {live.Total} unidades reconhecidas, {live.Itens.Count(s=>s.Latitude!=null)} coordenadas nesta página.");
}
class Source(string body):HttpMessageHandler{public int Calls;protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct){Calls++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body)});}}
