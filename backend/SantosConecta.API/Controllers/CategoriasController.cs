using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/categorias"), Authorize]
public class CategoriasController(CadastroService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<List<CategoriaDto>>> Listar() => Ok(await service.CategoriasAsync());
}
