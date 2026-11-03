using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Infrastructure.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/transito"), Authorize]
public class TransitoController(TransitoService service, ViaService vias) : ControllerBase
{
    [HttpGet("geometria")] public async Task<IActionResult> Geometria([FromQuery] string nome, CancellationToken ct) => Ok(await vias.ConsultarAsync(nome, ct));
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] public async Task<IActionResult> Estado([FromQuery] string? regiao, CancellationToken ct) => Ok(await service.ConsultarAsync(regiao, ct));
}
