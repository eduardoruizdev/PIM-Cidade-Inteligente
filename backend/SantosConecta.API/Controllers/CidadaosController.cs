using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/cidadaos"), Authorize]
public class CidadaosController(CadastroService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<List<CidadaoDto>>> Listar() => Ok(await service.CidadaosAsync());
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CidadaoDto>> Obter([FromRoute] int id)
    { var c = await service.CidadaoAsync(id); return c is null ? NotFound() : Ok(c); }
    [HttpPost]
    public async Task<ActionResult<CidadaoDto>> Criar([FromBody] CriarCidadaoDto dto)
    { var c = await service.CriarCidadaoAsync(dto); return CreatedAtAction(nameof(Obter), new { id = c.IdCidadao }, c); }
}
