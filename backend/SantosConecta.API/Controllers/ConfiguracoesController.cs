using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/configuracoes"), Authorize]
public class ConfiguracoesController(IConfiguration config) : ControllerBase
{
    [HttpGet]
    public IActionResult Obter() => Ok(new
    {
        EmailSimulado = config.GetValue("Email:Simular", true),
        GroqSimulada = config.GetValue("Groq:Simular", true)
    });
}
