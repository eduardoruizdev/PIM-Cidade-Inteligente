using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Infrastructure.Services;
namespace SantosConecta.API.Controllers;
[ApiController,Route("api/servicos-publicos"),Authorize]
public class ServicosPublicosController(ServicosPublicosService service):ControllerBase
{
    [HttpGet]public async Task<IActionResult> Lista(string? categoria,string? bairro,string? texto,double? latitude,double? longitude,double? raio,int limite=100,int inicio=0,CancellationToken ct=default)
    {try{return Ok(await service.Listar(categoria,bairro,texto,latitude,longitude,raio,limite,inicio,ct));}catch(ArgumentException ex){return BadRequest(new ProblemDetails{Status=400,Title="Filtros inválidos",Detail=ex.Message});}}
    [HttpGet("categorias")]public IActionResult Categorias()=>Ok(ServicosPublicosService.Categorias);
    [HttpGet("{id}")]public async Task<IActionResult> Detalhe(string id,CancellationToken ct)=>await service.Detalhe(id,ct) is {} item?Ok(item):NotFound();
}
