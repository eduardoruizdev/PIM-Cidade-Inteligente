using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/dashboard"), Authorize]
public class DashboardController(DashboardService service) : ControllerBase
{
    [HttpGet("resumo")]
    public async Task<ActionResult<DashboardResumoDto>> Resumo([FromQuery] string? status, [FromQuery] string? prioridade) =>
        Ok(await service.ResumoAsync(status, prioridade));
    [HttpGet("bairros")]
    public async Task<ActionResult<List<BairroDto>>> Bairros([FromQuery] string? status, [FromQuery] string? prioridade) =>
        Ok(await service.BairrosAsync(status, prioridade));
}
