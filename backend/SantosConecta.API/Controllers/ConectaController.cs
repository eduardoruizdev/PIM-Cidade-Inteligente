using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SantosConecta.Infrastructure.Services;
namespace SantosConecta.API.Controllers;

[ApiController,Route("api/conecta"),Authorize(Roles="Cidadao"),EnableRateLimiting("conecta")]
public class ConectaController(ConectaService service) : ControllerBase
{
    private int Cidadao=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public record Entrada([Required,StringLength(1000,MinimumLength=1)] string Mensagem);
    [HttpPost("mensagens"),RequestSizeLimit(8192)]
    public async Task<IActionResult> Enviar(Entrada input,CancellationToken ct)
    {
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(25));
        try{return Ok(await service.ConversarAsync(Cidadao,input.Mensagem,timeout.Token));}
        catch(OperationCanceledException) when(!ct.IsCancellationRequested){return StatusCode(503,new {detail="A consulta demorou mais que o esperado. Tente novamente em instantes."});}
    }
    [HttpGet("historico")]public async Task<IActionResult> Historico(CancellationToken ct)=>Ok(new {mensagens=await service.HistoricoAsync(Cidadao,ct),retencao="Temporário: até 20 mensagens, 24 horas ou 30 minutos sem uso. Reiniciar o servidor ou atingir o limite de memória pode encerrar o histórico."});
    [HttpDelete("historico")]public async Task<IActionResult> Limpar(CancellationToken ct){await service.LimparAsync(Cidadao,ct);return NoContent();}
}
