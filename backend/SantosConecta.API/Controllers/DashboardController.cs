using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/dashboard"), Authorize(Roles="Administrador,Funcionário")]
public class DashboardController(DashboardService service) : ControllerBase
{
    [HttpGet("resumo")]
    public async Task<ActionResult<DashboardResumoDto>> Resumo([FromQuery] string? status, [FromQuery] string? prioridade, [FromQuery] DateTime? inicio, [FromQuery] DateTime? fim) =>
        Ok(await service.ResumoAsync(status, prioridade, inicio, fim));
    [HttpGet("bairros")]
    public async Task<ActionResult<List<BairroDto>>> Bairros([FromQuery] string? status, [FromQuery] string? prioridade, [FromQuery] DateTime? inicio, [FromQuery] DateTime? fim) =>
        Ok(await service.BairrosAsync(status, prioridade, inicio, fim));
}
