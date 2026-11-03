using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/emails"), Authorize(Roles="Administrador,Funcionário")]
public class EmailsController(EmailService service, OcorrenciaService ocorrencias) : ControllerBase
{
    [HttpGet("ocorrencia/{id:int}")]
    public async Task<ActionResult<List<EmailDto>>> Historico([FromRoute] int id)
    {
        if (await ocorrencias.ObterAsync(id) is null) return NotFound();
        return Ok(await service.HistoricoAsync(id));
    }
    [HttpPost("enviar")]
    public async Task<ActionResult<EmailDto>> Enviar([FromBody] EnviarEmailDto dto)
    {
        var email = await service.EnviarAsync(dto, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        if (email is null) return NotFound();
        return email.StatusEnvio == "Falhou" ? StatusCode(StatusCodes.Status502BadGateway, email) : Ok(email);
    }
}
