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
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);
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
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.Cookie.Name = "SantosConecta.Auth"; o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(4); o.SlidingExpiration = true;
    o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
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
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AutenticacaoService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AssistenteService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddHttpClient<IRespostaService, GroqService>(c => c.Timeout = TimeSpan.FromSeconds(30));
var app = builder.Build();

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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", async (SantosConectaDbContext db) =>
{
    try { return await db.Database.CanConnectAsync() ? Results.Ok(new { status = "ok", database = "SQL Server" }) : Results.Json(new { status = "indisponivel", database = "SQL Server" }, statusCode: 503); }
    catch { return Results.Json(new { status = "indisponivel", database = "SQL Server" }, statusCode: 503); }
}).AllowAnonymous();
app.Run();
