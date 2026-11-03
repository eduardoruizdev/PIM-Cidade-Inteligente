using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/ocorrencias"), Authorize]
public class OcorrenciasController(OcorrenciaService service, PortalService portal) : ControllerBase
{
    private int Autor => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("minhas"), Authorize(Roles="Cidadao")]
    public async Task<IActionResult> Minhas() => Ok(await portal.MinhasAsync(Autor));
    [HttpGet("minhas/{id:int}"), Authorize(Roles="Cidadao")]
    public async Task<IActionResult> Minha(int id) {var o=await portal.MinhaAsync(id,Autor);return o is null ? NotFound() : Ok(o);}
    [HttpPost("minhas"), Authorize(Roles="Cidadao")]
    public async Task<IActionResult> CriarMinha([FromBody] NovaReclamacaoDto dto) => StatusCode(201,await portal.CriarAsync(Autor,dto));
    [HttpGet, Authorize(Roles="Administrador,Funcionário")]
    public async Task<ActionResult<List<OcorrenciaDto>>> Listar([FromQuery] string? status, [FromQuery] string? prioridade) =>
        Ok(await service.ListarAsync(status, prioridade));
    [HttpGet("{id:int}"), Authorize(Roles="Administrador,Funcionário,Cidadao")]
    public async Task<ActionResult<OcorrenciaDto>> Obter([FromRoute] int id)
    {
        if (User.IsInRole("Cidadao")) { var minha = await portal.MinhaAsync(id, Autor); return minha is null ? NotFound() : Ok(minha); }
        var o = await service.ObterAsync(id); return o is null ? NotFound() : Ok(o);
    }
    [HttpPost, Authorize(Roles="Administrador,Funcionário,Cidadao")]
    public async Task<IActionResult> Criar([FromBody] JsonElement body)
    {
        var options=new JsonSerializerOptions(JsonSerializerDefaults.Web);
        if(User.IsInRole("Cidadao"))
        {
            var entrada=body.Deserialize<NovaReclamacaoDto>(options);
            if(entrada is null)return BadRequest();
            if(!TryValidateModel(entrada))return ValidationProblem(ModelState);
            return StatusCode(201,await portal.CriarAsync(Autor,entrada));
        }
        var dto=body.Deserialize<SalvarOcorrenciaDto>(options);
        if(dto is null)return BadRequest();
        if(!TryValidateModel(dto))return ValidationProblem(ModelState);
        var o = await service.CriarAsync(dto, Autor);
        return CreatedAtAction(nameof(Obter), new { id = o.IdOcorrencia }, o);
    }
    [HttpPut("{id:int}"), Authorize(Roles="Administrador,Funcionário")]
    public async Task<ActionResult<OcorrenciaDto>> Editar([FromRoute] int id, [FromBody] SalvarOcorrenciaDto dto)
    {
        var o = await service.EditarAsync(id, dto, Autor); return o is null ? NotFound() : Ok(o);
    }
    [HttpPut("{id:int}/status"), Authorize(Roles="Administrador,Funcionário")]
    public async Task<ActionResult<OcorrenciaDto>> Status([FromRoute] int id, [FromBody] AtualizarStatusDto dto)
    {
        var o = await service.AlterarStatusAsync(id, dto.Status, Autor); return o is null ? NotFound() : Ok(o);
    }
    [HttpPost("{id:int}/historico"), Authorize(Roles="Administrador,Funcionário")]
    public async Task<IActionResult> Observar([FromRoute] int id, [FromBody] ObservacaoDto dto) =>
        await service.ObservarAsync(id, dto.Descricao, Autor) ? NoContent() : NotFound();
}
