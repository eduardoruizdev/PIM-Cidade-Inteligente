using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(AutenticacaoService service, PortalService portal) : ControllerBase
{
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<IActionResult> Entrar([FromBody] LoginDto dto, [FromQuery] string? publico = null)
    {
        if(publico=="cidadao")return await LoginCidadao(dto);
        var funcionario = await service.EntrarAsync(dto);
        if (funcionario is null) return Unauthorized(new { message = "E-mail ou senha inválidos." });
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, funcionario.IdFuncionario.ToString()),
            new Claim(ClaimTypes.Name, funcionario.Nome), new Claim(ClaimTypes.Email, funcionario.Email),
            new Claim(ClaimTypes.Role, funcionario.Perfil) };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return Ok(funcionario);
    }
    [HttpPost("cadastro"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<IActionResult> Cadastro([FromBody] CadastroCidadaoDto dto) => Ok(await portal.CadastrarAsync(dto));
    [HttpPost("cidadao/login"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<IActionResult> LoginCidadao([FromBody] LoginDto dto)
    {
        var c = await portal.LoginAsync(dto);
        if(c is null)return Unauthorized(new {message="E-mail ou senha inválidos."});
        var claims=new[] {new Claim(ClaimTypes.NameIdentifier,c.IdCidadao.ToString()),new Claim(ClaimTypes.Name,c.Nome),new Claim(ClaimTypes.Role,"Cidadao")};
        await HttpContext.SignInAsync("Cidadao",new ClaimsPrincipal(new ClaimsIdentity(claims,"Cidadao")));
        return Ok(c);
    }
    [HttpGet("me"), Authorize(Roles="Administrador,Funcionário")]
    public ActionResult<FuncionarioDto> Me() => Ok(new FuncionarioDto(
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.Identity!.Name!,
        User.FindFirstValue(ClaimTypes.Email)!, User.FindFirstValue(ClaimTypes.Role)!));
    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Sair()
    { await HttpContext.SignOutAsync(User.IsInRole("Cidadao") ? "Cidadao" : CookieAuthenticationDefaults.AuthenticationScheme); return NoContent(); }
}
