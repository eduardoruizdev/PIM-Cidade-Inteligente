using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/assistente"), Authorize]
public class AssistenteController(AssistenteService service) : ControllerBase
{
    [HttpPost("gerar-resposta")]
    public async Task<ActionResult<SugestaoDto>> Gerar([FromBody] GerarRespostaDto dto, CancellationToken cancellationToken)
    {
        var sugestao = await service.GerarAsync(dto.IdOcorrencia, cancellationToken);
        return sugestao is null ? NotFound() : Ok(sugestao);
    }
}
