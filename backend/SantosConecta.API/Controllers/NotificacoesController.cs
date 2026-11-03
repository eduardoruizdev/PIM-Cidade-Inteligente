using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/notificacoes"), Authorize(Roles = "Cidadao,Administrador,Funcionário")]
public class NotificacoesController(MensagemService service) : ControllerBase
{
    private int Autor => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Tipo => User.IsInRole("Cidadao") ? "Cidadao" : "Equipe";
    private bool Administrador => User.IsInRole("Administrador");
    [HttpGet] public async Task<IActionResult> Listar() => Ok(await service.NotificacoesAsync(Autor, Tipo, Administrador));
    [HttpGet("nao-lidas/quantidade")] public async Task<IActionResult> Quantidade() => Ok(new { quantidade = await service.QuantidadeAsync(Autor, Tipo, Administrador) });
    [HttpPut("{id:int}/lida")] public async Task<IActionResult> Lida(int id) => await service.MarcarNotificacaoAsync(id, Autor, Tipo, Administrador) ? NoContent() : NotFound();
}
