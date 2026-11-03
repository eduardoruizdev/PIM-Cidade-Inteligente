using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SantosConecta.Domain.Entities;
using SantosConecta.Infrastructure.Data;
using SantosConecta.Infrastructure.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/clima"), Authorize]
public class ClimaController(ClimaService clima, SantosConectaDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Atual(CancellationToken ct) => Ok(await clima.ConsultarAsync(ct));
    [HttpGet("previsao")] public async Task<IActionResult> Previsao(CancellationToken ct) => Ok(await clima.ConsultarAsync(ct));
    [HttpGet("alertas")] public async Task<IActionResult> Alertas(CancellationToken ct) => Ok(await db.AlertasMeteorologicos.AsNoTracking().Where(a => a.Ativo && a.Fim > DateTime.UtcNow).OrderBy(a => a.Inicio).ToListAsync(ct));
    [HttpGet("alertas/historico")] public async Task<IActionResult> Historico([FromQuery] int pagina = 1, CancellationToken ct = default) => Ok(await db.AlertasMeteorologicos.AsNoTracking().OrderByDescending(a => a.PublicadoEm).ThenBy(a=>a.Id).Skip((Math.Clamp(pagina,1,10000)-1)*50).Take(50).ToListAsync(ct));
    [HttpGet("alertas/{id}")] public async Task<IActionResult> Detalhe(string id, CancellationToken ct) => await db.AlertasMeteorologicos.AsNoTracking().FirstOrDefaultAsync(a=>a.Id==id,ct) is {} a ? Ok(a) : NotFound();
    private int Cidadao => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("preferencias"), Authorize(Roles="Cidadao")]
    public async Task<IActionResult> Preferencias(CancellationToken ct)
    {
        var p = await db.PreferenciasMeteorologicas.FindAsync([Cidadao], ct);
        return Ok(new {habilitada=p?.Habilitada??false, dispositivoRegistrado=p?.Token!=null});
    }
    public record PreferenciaInput(bool Habilitada, [MaxLength(2048)] string? Token);
    [HttpPut("preferencias"), Authorize(Roles="Cidadao")]
    public async Task<IActionResult> Salvar(PreferenciaInput input, CancellationToken ct)
    {
        if(input.Habilitada && string.IsNullOrWhiteSpace(input.Token)) return BadRequest(new {detail="Autorize as notificações neste dispositivo primeiro."});
        var p = await db.PreferenciasMeteorologicas.FindAsync([Cidadao],ct);
        if(p is null) {p=new(){IdCidadao=Cidadao};db.PreferenciasMeteorologicas.Add(p);}
        if(input.Habilitada) {
            var others=await db.PreferenciasMeteorologicas.Where(x=>x.IdCidadao!=Cidadao && x.Token==input.Token).ToListAsync(ct);
            foreach(var other in others) {other.Token=null;other.Habilitada=false;}
        }
        p.Habilitada=input.Habilitada;p.Token=input.Habilitada?input.Token:null;p.AtualizadaEm=DateTime.UtcNow;
        await db.SaveChangesAsync(ct);return NoContent();
    }
}
