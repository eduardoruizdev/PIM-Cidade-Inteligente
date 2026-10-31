using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/funcionarios"), Authorize]
public class FuncionariosController(CadastroService service) : ControllerBase
{
    // Identificação dos responsáveis disponível aos dois perfis; gestão de equipe no frontend é administrativa.
    [HttpGet] public async Task<ActionResult<List<FuncionarioDto>>> Listar() => Ok(await service.FuncionariosAsync());
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FuncionarioDto>> Obter([FromRoute] int id)
    { var f = await service.FuncionarioAsync(id); return f is null ? NotFound() : Ok(f); }
}
