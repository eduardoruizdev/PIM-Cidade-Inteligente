using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/cidadaos"), Authorize]
public class CidadaosController(CadastroService service, PortalService portal) : ControllerBase
{
    private int Identidade => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    [HttpGet("me"), Authorize(Roles="Cidadao")]
    public async Task<IActionResult> Me() {var c=await portal.PerfilAsync(Identidade);return c is null ? NotFound() : Ok(c);}
    [HttpPut("me"), Authorize(Roles="Cidadao")]
    public async Task<IActionResult> EditarMe([FromBody] PerfilCidadaoDto dto) {var c=await portal.EditarAsync(Identidade,dto);return c is null ? NotFound() : Ok(c);}
    [HttpGet, Authorize(Roles="Administrador,Funcionário")] public async Task<ActionResult<List<CidadaoDto>>> Listar() => Ok(await service.CidadaosAsync());
    [HttpGet("{id:int}"), Authorize(Roles="Administrador,Funcionário")]
    public async Task<ActionResult<CidadaoDto>> Obter([FromRoute] int id)
    { var c = await service.CidadaoAsync(id); return c is null ? NotFound() : Ok(c); }
    [HttpPost, Authorize(Roles="Administrador,Funcionário")]
    public async Task<ActionResult<CidadaoDto>> Criar([FromBody] CriarCidadaoDto dto)
    { var c = await service.CriarCidadaoAsync(dto); return CreatedAtAction(nameof(Obter), new { id = c.IdCidadao }, c); }
}
