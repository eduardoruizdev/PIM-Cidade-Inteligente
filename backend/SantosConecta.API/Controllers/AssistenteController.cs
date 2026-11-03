using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/assistente"), Authorize]
public class AssistenteController(AssistenteService service, IRespostaService resposta, CadastroService cadastro) : ControllerBase
{
    [HttpPost("classificar"), Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public async Task<IActionResult> Classificar([FromBody] ClassificarDto dto,CancellationToken cancellationToken)
    {
        string[] permitidas=["Buracos nas vias","Iluminação pública","Coleta de lixo","Calçadas","Drenagem","Sinalização","Outros"];
        var categorias=(await cadastro.CategoriasAsync()).Where(c=>permitidas.Contains(c.Nome)).ToList();
        return Ok(await resposta.ClassificarAsync(dto.Descricao,categorias,cancellationToken));
    }
    [HttpPost("analisar/{id:int}"), Authorize(Roles="Administrador,Funcionário")]
    public async Task<IActionResult> Analisar(int id,[FromQuery]string tarefa,CancellationToken ct) {
        if(tarefa!="resumo"&&tarefa!="classificacao"&&tarefa!="faltantes")return BadRequest(new {message="Tarefa inválida."});
        var resultado=await service.AnalisarAsync(id,tarefa,ct);return resultado is null?NotFound():Ok(resultado);
    }
    [HttpPost("gerar-resposta"), Authorize(Roles="Administrador,Funcionário")]
    public async Task<ActionResult<SugestaoDto>> Gerar([FromBody] GerarRespostaDto dto, CancellationToken cancellationToken)
    {
        var sugestao = await service.GerarAsync(dto.IdOcorrencia, cancellationToken);
        return sugestao is null ? NotFound() : Ok(sugestao);
    }
}
