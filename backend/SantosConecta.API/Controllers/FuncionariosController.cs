using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/funcionarios"), Authorize(Roles = "Administrador,Funcionário")]
public class FuncionariosController(CadastroService service, EquipeService equipe) : ControllerBase
{
    [HttpPost, Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Criar(SalvarFuncionarioDto dto) => StatusCode(201, await equipe.SalvarAsync(null, dto, int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value)));
    [HttpPut("{id:int}"), Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Editar(int id, SalvarFuncionarioDto dto) { var f = await equipe.SalvarAsync(id, dto, int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value)); return f is null ? NotFound() : Ok(f); }
    // Identificação dos responsáveis disponível aos dois perfis; gestão de equipe no frontend é administrativa.
    [HttpGet("responsaveis")] public async Task<IActionResult> Responsaveis() => Ok((await service.FuncionariosAsync()).Where(f => f.Ativo).Select(f => new { f.IdFuncionario, f.Nome, f.Perfil, f.Cargo }));
    [HttpGet, Authorize(Roles = "Administrador")] public async Task<ActionResult<List<FuncionarioDto>>> Listar() => Ok(await service.FuncionariosAsync());
    [HttpGet("{id:int}"), Authorize(Roles = "Administrador")]
    public async Task<ActionResult<FuncionarioDto>> Obter([FromRoute] int id)
    { var f = await service.FuncionarioAsync(id); return f is null ? NotFound() : Ok(f); }
}
