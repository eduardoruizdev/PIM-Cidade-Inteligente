using Microsoft.AspNetCore.Authentication.Cookies;
using SantosConecta.Domain.Repositories;
namespace SantosConecta.API.Authentication;
public static class PortalAuthentication
{
    public static IServiceCollection AddPortalAuthentication(this IServiceCollection services, bool development)
    {
services.AddAuthentication(o => {o.DefaultScheme="Portal";o.DefaultSignInScheme=CookieAuthenticationDefaults.AuthenticationScheme;})
.AddPolicyScheme("Portal", "Seleção do portal", o => o.ForwardDefaultSelector = context =>
    context.Request.Headers["X-Santos-Client"] == "citizen" ||
    (context.Request.Headers["X-Santos-Client"] != "staff" && !context.Request.Cookies.ContainsKey("SantosConecta.Auth") && context.Request.Cookies.ContainsKey("SantosConecta.Citizen"))
        ? "Cidadao" : CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(o =>
{
    o.Cookie.Name = "SantosConecta.Auth"; o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = development ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(4); o.SlidingExpiration = true;
    o.Events.OnValidatePrincipal = async c => {
        if(c.Principal?.IsInRole("Cidadao") == true) {c.RejectPrincipal();return;}
        var raw=c.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var repo=c.HttpContext.RequestServices.GetRequiredService<IFuncionarioRepository>();
        var f=int.TryParse(raw,out var id) ? await repo.ObterAsync(id) : null;
        if(f is null || !f.Ativo || c.Principal?.IsInRole(f.Perfil) != true) c.RejectPrincipal();
    };
    o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
}).AddCookie("Cidadao", o =>
{
    o.Cookie.Name="SantosConecta.Citizen";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Lax;
    o.Cookie.SecurePolicy=development?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;
    o.ExpireTimeSpan=TimeSpan.FromHours(4);o.SlidingExpiration=true;
    o.Events.OnValidatePrincipal=c=> {if(c.Principal?.IsInRole("Cidadao")!=true)c.RejectPrincipal();return Task.CompletedTask;};
    o.Events.OnRedirectToLogin=c=> {c.Response.StatusCode=401;return Task.CompletedTask;};
    o.Events.OnRedirectToAccessDenied=c=> {c.Response.StatusCode=403;return Task.CompletedTask;};
});

        return services;
    }
}