using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/priorizacao"), Authorize(Roles="Administrador,Funcionário")]
public class PriorizacaoController(PriorizacaoService service):ControllerBase
{
    [HttpGet("fila")] public async Task<IActionResult> Fila()=>Ok(await service.FilaAsync());
    [HttpGet("indicadores")] public async Task<IActionResult> Indicadores()=>Ok(await service.IndicadoresAsync());
    [HttpGet("equidade")] public async Task<IActionResult> Equidade()=>Ok(await service.EquidadeAsync());
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Revisar(int id,[FromBody] RevisarPrioridadeDto dto)=>await service.RevisarAsync(id,dto,int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) ? NoContent() : NotFound();
}
