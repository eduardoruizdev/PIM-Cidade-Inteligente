using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SantosConecta.Application.Services;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
using SantosConecta.Infrastructure.Data;
using SantosConecta.Infrastructure.Repositories;
using SantosConecta.Infrastructure.Services;
using SantosConecta.API.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// A exceção de certificado e o arquivo local são exclusivos de Development.
if (builder.Environment.IsDevelopment())
{
    // Nome de configuração local preservado a pedido do usuário; ambos ignorados no Git.
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);
    builder.Configuration.AddJsonFile("appsettings.Local.example.json", optional: true);
}
builder.Configuration.AddEnvironmentVariables();
var connectionString = builder.Configuration.GetConnectionString("SantosConecta")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:SantosConecta no servidor.");
var connectionOptions = new SqlConnectionStringBuilder(connectionString);
if (!builder.Environment.IsDevelopment() && connectionOptions.TrustServerCertificate)
    throw new InvalidOperationException("TrustServerCertificate=True só é permitido no ambiente Development. Configure um certificado confiável fora do desenvolvimento.");
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "Santos Conecta — API acadêmica",
    Version = "v1",
    Description = "Dados fictícios. Faça login em /api/auth/login para utilizar os endpoints protegidos nesta mesma origem."
}));
var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".data-protection")));
SantosConecta.API.Authentication.PortalAuthentication.AddPortalAuthentication(builder.Services, builder.Environment.IsDevelopment());
builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("TravessiaLinks", c => {
    c.Timeout = TimeSpan.FromSeconds(16);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("SantosConecta/3.0 (verificacao-de-link)");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton(sp => new TravessiaService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("TravessiaLinks"),
    sp.GetRequiredService<ILogger<TravessiaService>>()));
builder.Services.AddHttpClient<ServicosPublicosService>(c=>{c.Timeout=TimeSpan.FromSeconds(20);c.DefaultRequestHeaders.UserAgent.ParseAdd("SantosConecta-Academico/3.0");});
builder.Services.AddSingleton<ConectaHistorico>();
builder.Services.AddScoped<IConectaConsultas,ConectaConsultas>();
builder.Services.AddScoped<ConectaService>();
builder.Services.AddHttpClient<IConectaModelo,ConectaModelo>(c=>c.Timeout=TimeSpan.FromSeconds(12)).RemoveAllLoggers();
builder.Services.AddHttpClient<ClimaService>(c=>c.Timeout=TimeSpan.FromSeconds(20));
builder.Services.AddHttpClient<ViaService>(c=> { c.Timeout=TimeSpan.FromSeconds(35);c.DefaultRequestHeaders.UserAgent.ParseAdd("SantosConecta/2.0"); });
builder.Services.AddHttpClient<FcmMeteorologia>(c=>c.Timeout=TimeSpan.FromSeconds(20)).RemoveAllLoggers();
builder.Services.AddHostedService<MeteorologiaWorker>();
builder.Services.AddHttpClient<TransitoService>(c=>c.Timeout=TimeSpan.FromSeconds(15)).RemoveAllLoggers();
builder.Services.AddHttpClient<LocalizacaoService>(c => { c.BaseAddress=new Uri(builder.Configuration["Geocoding:BaseUrl"]??"https://nominatim.openstreetmap.org/");c.Timeout=TimeSpan.FromSeconds(20);c.DefaultRequestHeaders.UserAgent.ParseAdd("SantosConecta-Academico/2.0"); });
builder.Services.AddScoped<IPortalRepository, PortalRepository>();
builder.Services.AddScoped<IPasswordHasher<Cidadao>, PasswordHasher<Cidadao>>();
builder.Services.AddScoped<PortalService>();
builder.Services.AddScoped<PriorizacaoService>();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("conecta", context => RateLimitPartition.GetFixedWindowLimiter(
        (context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonimo")+":"+(context.User.IsInRole("Cidadao")?"cidadao":"outro"),
        _=>new FixedWindowRateLimiterOptions{PermitLimit=12,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
    o.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddDbContext<SantosConectaDbContext>(o => o.UseSqlServer(connectionString));
builder.Services.AddScoped<IOcorrenciaRepository, OcorrenciaRepository>();
builder.Services.AddScoped<ICidadaoRepository, CidadaoRepository>();
builder.Services.AddScoped<IFuncionarioRepository, FuncionarioRepository>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddScoped<IPasswordHasher<Funcionario>, PasswordHasher<Funcionario>>();
builder.Services.AddScoped<OcorrenciaService>();
builder.Services.AddScoped<CadastroService>();
builder.Services.AddScoped<EquipeService>();
builder.Services.AddScoped<MensagemService>();
builder.Services.AddScoped<IMensagemRepository, MensagemRepository>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AutenticacaoService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AssistenteService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddHttpClient<IRespostaService, GroqService>(c => c.Timeout = TimeSpan.FromSeconds(30));
var app = builder.Build();

// Fixture explícita por CLI em Development: conta de funcionário, sem privilégios de administrador.
if (args.Contains("--preparar-teste"))
{
    if(!app.Environment.IsDevelopment())throw new InvalidOperationException("Fixture disponível apenas em Development.");
    var email=builder.Configuration["TestFixture:Email"]??"";
    var senha=builder.Configuration["TestFixture:Password"]??"";
    if(!email.EndsWith("@exemplo.invalid")||senha.Length<20)throw new ArgumentException("Configure credenciais exclusivas da fixture no ambiente.");
    using var scope=app.Services.CreateScope();
    var db=scope.ServiceProvider.GetRequiredService<SantosConectaDbContext>();
    if(await db.Funcionarios.AnyAsync(f=>f.Email==email))throw new ArgumentException("Fixture já existe; use um e-mail exclusivo.");
    var perfil=builder.Configuration["TestFixture:Perfil"]??"Funcionário";
    if(perfil!="Funcionário" && perfil!="Administrador")throw new ArgumentException("Perfil de fixture inválido.");
    var f=new Funcionario {Nome="Funcionário de teste acadêmico",Email=email,Perfil=perfil,Cargo="Validação acadêmica",Telefone="13999999999"};
    f.SenhaHash=scope.ServiceProvider.GetRequiredService<IPasswordHasher<Funcionario>>().HashPassword(f,senha);
    db.Funcionarios.Add(f);await db.SaveChangesAsync();
    app.Logger.LogInformation("Fixture de teste criada. Nenhuma credencial registrada em log.");
    return;
}

if (args.Contains("--diagnosticar-sql"))
{
    Environment.ExitCode = await SqlConnectionDiagnostic.ExecutarAsync(app.Services, builder.Configuration, builder.Environment.EnvironmentName);
    return;
}

if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    await DataSeeder.PopularAsync(scope.ServiceProvider.GetRequiredService<SantosConectaDbContext>(), builder.Configuration["Seed:Password"] ?? "");
    app.Logger.LogInformation("Seed concluído: 132 ocorrências fictícias. Nenhum e-mail foi enviado.");
    return;
}

// Tratamento simples e centralizado; nunca devolver senha, chave, stack trace ou string de conexão.
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (Exception ex)
    {
        var status = ex switch
        {
            ArgumentException => 400,
            System.Text.Json.JsonException => 400,
            SqlException or DbUpdateException => 503,
            HttpRequestException => 502,
            InvalidOperationException => 503,
            OperationCanceledException => 504,
            _ => 500
        };
        app.Logger.LogError("Erro HTTP {Status}: {Tipo}", status, ex.GetType().Name);
        var detail = ex is ArgumentException ? ex.Message : status == 503 ? "Serviço indisponível. Verifique a configuração e a conexão com o SQL Server ou com a integração solicitada." : "Não foi possível concluir a operação. Tente novamente.";
        context.Response.StatusCode = status;
        await Results.Problem(statusCode: status, title: "Falha na operação", detail: detail).ExecuteAsync(context);
    }
});
app.UseCors("Frontend");
// JSON obrigatório nos controllers e verificação de Origin nas mutações protegem o cookie contra requisições de outros sites.
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers.Origin.ToString();
    var unsafeMethod = !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method);
    var sameOrigin = $"{context.Request.Scheme}://{context.Request.Host}";
    if (unsafeMethod && origin.Length > 0 && origin != sameOrigin && !origins.Contains(origin))
    {
        await Results.Problem(statusCode: 403, title: "Origem não autorizada",
            detail: "O endereço do frontend não está autorizado pela API. Verifique a porta configurada em AllowedOrigins e reinicie o backend.").ExecuteAsync(context);
        return;
    }
    await next();
});
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", async (SantosConectaDbContext db) =>
{
    try { return await db.Database.CanConnectAsync() ? Results.Ok(new { status = "ok", database = "SQL Server" }) : Results.Json(new { status = "indisponivel", database = "SQL Server" }, statusCode: 503); }
    catch { return Results.Json(new { status = "indisponivel", database = "SQL Server" }, statusCode: 503); }
}).AllowAnonymous();
app.Run();
