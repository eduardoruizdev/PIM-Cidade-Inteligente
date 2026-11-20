using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SantosConecta.Infrastructure.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/transito"), Authorize]
public class TransitoController(TransitoService service, ViaService vias) : ControllerBase
{
    [HttpGet("geometria")] public async Task<IActionResult> Geometria([FromQuery] string nome, CancellationToken ct) => Ok(await vias.ConsultarAsync(nome, ct));
    [HttpGet("tiles/flow/{zoom:int}/{x:int}/{y:int}"), EnableRateLimiting("traffic-tiles"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> TileFluxo([FromRoute] int zoom, [FromRoute] int x, [FromRoute] int y, CancellationToken ct)
    {
        if (zoom is < 5 or > 22 || x < 0 || y < 0 || x >= (1 << zoom) || y >= (1 << zoom)) return BadRequest();
        var tile = await service.ConsultarTileFluxoAsync(zoom, x, y, ct);
        return tile is null ? StatusCode(503, new { message = "Tile de trânsito indisponível." }) : File(tile, "image/png");
    }
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] public async Task<IActionResult> Estado([FromQuery] string? regiao, CancellationToken ct) => Ok(await service.ConsultarAsync(regiao, ct));
}
