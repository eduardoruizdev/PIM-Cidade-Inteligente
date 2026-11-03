using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/ocorrencias/{id:int}/mensagens"), Authorize(Roles = "Administrador,Funcionário,Cidadao")]
public class MensagensController(MensagemService service) : ControllerBase
{
    private int Autor => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Tipo => User.IsInRole("Cidadao") ? "Cidadao" : "Equipe";
    private Task<bool> Acesso(int id) => service.PodeAcessarAsync(id, Tipo == "Cidadao" ? Autor : null);
    [HttpGet] public async Task<IActionResult> Listar(int id) => await Acesso(id) ? Ok(await service.ListarAsync(id, Autor, Tipo)) : NotFound();
    [HttpPost]
    public async Task<IActionResult> Enviar(int id, NovaMensagemDto dto)
    {
        if (!await Acesso(id)) return NotFound();
        await service.EnviarAsync(id, Autor, Tipo, User.FindFirstValue(ClaimTypes.Name) ?? Tipo, dto.Conteudo); return StatusCode(201, new { enviado = true });
    }
    [HttpPost("lidas")] public async Task<IActionResult> Lidas(int id) { if (!await Acesso(id)) return NotFound(); await service.LidasAsync(id, Tipo, Autor, User.IsInRole("Administrador")); return NoContent(); }
}
