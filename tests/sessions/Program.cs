using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using SantosConecta.API.Authentication;
using SantosConecta.API.Controllers;
using SantosConecta.Infrastructure.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

// Servidor efêmero só de teste: não inicia Program da API nem conecta ao SQL.
var builder=WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddPortalAuthentication(true);
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddAuthorization();
builder.Services.AddControllers().AddApplicationPart(typeof(ConectaController).Assembly);
builder.Services.AddSingleton<ConectaHistorico>();
builder.Services.AddScoped<IConectaModelo,NoModel>();
builder.Services.AddScoped<IConectaConsultas,NoData>();
builder.Services.AddScoped<ConectaService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<TravessiaService>();
builder.Services.AddHttpClient<ServicosPublicosService>().ConfigurePrimaryHttpMessageHandler(()=>new PublicSource());
builder.Services.AddRateLimiter(o=> {o.RejectionStatusCode=429;o.AddFixedWindowLimiter("conecta",l=>{l.PermitLimit=50;l.Window=TimeSpan.FromMinutes(1);l.QueueLimit=0;});});
// Fixture de funcionário: remove apenas a consulta SQL deste host de teste.
builder.Services.PostConfigure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme,o=>o.Events.OnValidatePrincipal=_=>Task.CompletedTask);
await using var app=builder.Build();
app.UseAuthentication();app.UseRateLimiter();app.UseAuthorization();app.MapControllers();
app.MapGet("/fixture/{type}",async (string type,HttpContext c)=>{
    var citizen=type=="citizen";
    var scheme=citizen?"Cidadao":CookieAuthenticationDefaults.AuthenticationScheme;
    await c.SignInAsync(scheme,new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,citizen?"2":"1"),new Claim(ClaimTypes.Role,citizen?"Cidadao":"Funcionário")},scheme)));
    return Results.Ok();
});
app.MapGet("/staff",()=>"staff").RequireAuthorization(p=>p.RequireRole("Funcionário"));
app.MapGet("/citizen",()=>"citizen").RequireAuthorization(p=>p.RequireRole("Cidadao"));
app.MapPost("/logout",async(HttpContext c)=>{await c.SignOutAsync(c.User.IsInRole("Cidadao")?"Cidadao":CookieAuthenticationDefaults.AuthenticationScheme);return Results.NoContent();}).RequireAuthorization();
await app.StartAsync();
using var client=new HttpClient(new HttpClientHandler{UseCookies=true,AllowAutoRedirect=false}){BaseAddress=new Uri(app.Urls.Single())};
void Check(bool result,string name){if(!result)throw new Exception(name);Console.WriteLine("OK: "+name);}
Check((await client.GetAsync("/fixture/staff")).IsSuccessStatusCode,"Fixture da equipe autenticada");
Check((await client.GetAsync("/fixture/citizen")).IsSuccessStatusCode,"Fixture cidadã autenticada");
client.DefaultRequestHeaders.Add("X-Santos-Client","staff");
Check((await client.GetAsync("/staff")).StatusCode==HttpStatusCode.OK,"Login cidadão preserva sessão da equipe no mesmo host");
Check((await client.GetAsync("/citizen")).StatusCode==HttpStatusCode.Forbidden,"Equipe não acessa recurso exclusivo do cidadão");
Check((await client.GetAsync("/api/conecta/historico")).StatusCode==HttpStatusCode.Forbidden,"Conecta não está disponível à equipe administrativa");
client.DefaultRequestHeaders.Remove("X-Santos-Client");client.DefaultRequestHeaders.Add("X-Santos-Client","citizen");
Check((await client.GetAsync("/citizen")).StatusCode==HttpStatusCode.OK,"Seleção do cidadão com ambos os cookies presentes");
using (var ferry = await client.GetAsync("/api/travessias/santos-guaruja")) {
    var json = System.Text.Json.JsonDocument.Parse(await ferry.Content.ReadAsStringAsync());
    Check(ferry.StatusCode == HttpStatusCode.OK && json.RootElement.GetProperty("sentidos").GetArrayLength() == 2,
        "Travessia responde via HTTP autenticado com ambos os sentidos");
}
Check((await client.GetAsync("/api/travessias/santos-guaruja/historico?periodo=7dias")).StatusCode == HttpStatusCode.OK,
    "Histórico de travessia responde via HTTP sem exigir SQL");
Check((await client.GetAsync("/api/servicos-publicos/categorias")).StatusCode==HttpStatusCode.OK,"Endpoint de categorias públicas autenticado");
Check((await client.GetAsync("/api/servicos-publicos?categoria=ubs&bairro=Centro&texto=fixture")).StatusCode==HttpStatusCode.OK,"Endpoint lista serviços com filtros");
Check((await client.GetAsync("/api/servicos-publicos/santos-1")).StatusCode==HttpStatusCode.OK,"Endpoint de detalhes do catálogo");
Check((await client.GetAsync("/api/servicos-publicos/santos-999")).StatusCode==HttpStatusCode.NotFound,"Unidade inexistente retorna 404");
Check((await client.GetAsync("/api/servicos-publicos?latitude=0&raio=10")).StatusCode==HttpStatusCode.BadRequest,"Endpoint rejeita coordenadas incompletas");
Check((await client.GetAsync("/staff")).StatusCode==HttpStatusCode.Forbidden,"Cidadão não acessa recurso exclusivo da equipe");
Check((await client.GetAsync("/api/conecta/historico")).StatusCode==HttpStatusCode.OK,"Cidadão autenticado consulta seu histórico do Conecta");
using(var response=await client.PostAsync("/api/conecta/mensagens",new StringContent("{\"mensagem\":\"oi\"}",System.Text.Encoding.UTF8,"application/json")))Check(response.StatusCode==HttpStatusCode.OK,"Envio e resposta pelo controller real do Conecta");
using(var response=await client.PostAsync("/api/conecta/mensagens",new StringContent("{\"mensagem\":\""+new string('a',1001)+"\"}",System.Text.Encoding.UTF8,"application/json")))Check(response.StatusCode==HttpStatusCode.BadRequest,"Controller recusa entrada acima do limite");
Check((await client.DeleteAsync("/api/conecta/historico")).StatusCode==HttpStatusCode.NoContent,"Controller limpa histórico autenticado");
await client.PostAsync("/logout",null);
Check((await client.GetAsync("/citizen")).StatusCode==HttpStatusCode.Unauthorized,"Logout invalida sessão cidadã");
Check((await client.GetAsync("/api/travessias/santos-guaruja")).StatusCode == HttpStatusCode.Unauthorized,
    "Travessia recusa acesso anônimo");
Check((await client.GetAsync("/api/conecta/historico")).StatusCode==HttpStatusCode.Unauthorized,"Conecta recusa acesso anônimo");
Check((await client.GetAsync("/api/servicos-publicos")).StatusCode==HttpStatusCode.Unauthorized,"Serviços preservam autenticação da API");
client.DefaultRequestHeaders.Remove("X-Santos-Client");client.DefaultRequestHeaders.Add("X-Santos-Client","staff");
Check((await client.GetAsync("/staff")).StatusCode==HttpStatusCode.OK,"Logout cidadão preserva sessão da equipe");
await client.GetAsync("/fixture/citizen");
client.DefaultRequestHeaders.Remove("X-Santos-Client");client.DefaultRequestHeaders.Add("X-Santos-Client","citizen");
var limited=false;
for(var i=0;i<55;i++){if((int)(await client.GetAsync("/api/conecta/historico")).StatusCode==429){limited=true;break;}}
Check(limited,"Endpoint do Conecta aplica rate limiting HTTP");
await app.StopAsync();
Console.WriteLine("24 verificações HTTP de sessões, Conecta, serviços e travessia aprovadas. Sem SQL e sem mensagens reais.");
class PublicSource:HttpMessageHandler {
 protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("""
 [{"type":"local","title":"Policlínica Fixture","nid":"1","field_endereco":"Rua Fixture - Centro","field_ponto":"-23.96,-46.33"}]
 """)});
}
class NoModel:IConectaModelo {public Task<PlanoConecta?> InterpretarAsync(int id,string m,MensagemConecta[] c,CancellationToken ct)=>Task.FromResult<PlanoConecta?>(null);}
class NoData:IConectaConsultas {
 public Task<List<OcorrenciaConecta>> MinhasAsync(int id,CancellationToken ct)=>throw new InvalidOperationException();
 public Task<RespostaEquipeConecta> RespostasAsync(int id,int o,CancellationToken ct)=>throw new InvalidOperationException();
 public Task<DadosClima> ClimaAsync(CancellationToken ct)=>throw new InvalidOperationException();
 public Task<EstadoTransito> TransitoAsync(string r,CancellationToken ct)=>throw new InvalidOperationException();
 public Task<List<SantosConecta.Domain.Entities.AlertaMeteorologico>> AlertasAsync(CancellationToken ct)=>throw new InvalidOperationException();
}
