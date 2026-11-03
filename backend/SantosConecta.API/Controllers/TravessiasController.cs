using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Infrastructure.Services;

namespace SantosConecta.API.Controllers;

[ApiController, Authorize, Route("api/travessias/santos-guaruja")]
public class TravessiasController(TravessiaService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Atual(CancellationToken ct = default) => Ok(await service.ConsultarAsync(ct));

    [HttpGet("historico")]
    public IActionResult Historico([FromQuery] string periodo = "horas") => Ok(service.Historico(periodo));
}
