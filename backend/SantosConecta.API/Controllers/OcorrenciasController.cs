using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/ocorrencias"), Authorize]
public class OcorrenciasController(OcorrenciaService service) : ControllerBase
{
    private int Autor => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet]
    public async Task<ActionResult<List<OcorrenciaDto>>> Listar([FromQuery] string? status, [FromQuery] string? prioridade) =>
        Ok(await service.ListarAsync(status, prioridade));
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OcorrenciaDto>> Obter([FromRoute] int id)
    {
        var o = await service.ObterAsync(id); return o is null ? NotFound() : Ok(o);
    }
    [HttpPost]
    public async Task<ActionResult<OcorrenciaDto>> Criar([FromBody] SalvarOcorrenciaDto dto)
    {
        var o = await service.CriarAsync(dto, Autor);
        return CreatedAtAction(nameof(Obter), new { id = o.IdOcorrencia }, o);
    }
    [HttpPut("{id:int}")]
    public async Task<ActionResult<OcorrenciaDto>> Editar([FromRoute] int id, [FromBody] SalvarOcorrenciaDto dto)
    {
        var o = await service.EditarAsync(id, dto, Autor); return o is null ? NotFound() : Ok(o);
    }
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<OcorrenciaDto>> Status([FromRoute] int id, [FromBody] AtualizarStatusDto dto)
    {
        var o = await service.AlterarStatusAsync(id, dto.Status, Autor); return o is null ? NotFound() : Ok(o);
    }
    [HttpPost("{id:int}/historico")]
    public async Task<IActionResult> Observar([FromRoute] int id, [FromBody] ObservacaoDto dto) =>
        await service.ObservarAsync(id, dto.Descricao, Autor) ? NoContent() : NotFound();
}
