using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Infrastructure.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/localizacao"), Authorize]
public class LocalizacaoController(LocalizacaoService service) : ControllerBase
{
    [HttpGet("bairros")] public IActionResult Bairros() => Ok(LocalizacaoService.Bairros);
    [HttpGet("buscar")] public async Task<IActionResult> Buscar([FromQuery] string bairro, [FromQuery] string rua, [FromQuery] string? numero) => Ok(await service.BuscarAsync(bairro, rua, numero));
}
