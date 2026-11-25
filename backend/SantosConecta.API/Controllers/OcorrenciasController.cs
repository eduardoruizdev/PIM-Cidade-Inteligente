using System.Security.Claims;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SantosConecta.Application.DTOs;
using SantosConecta.Application.Services;
using SantosConecta.Infrastructure.Services;
namespace SantosConecta.API.Controllers;

[ApiController, Route("api/ocorrencias"), Authorize]
public class OcorrenciasController(OcorrenciaService service, PortalService portal, FotosOcorrenciaService fotos, ILogger<OcorrenciasController> logger) : ControllerBase
{
    private int Autor => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("minhas"), Authorize(Roles="Cidadao")]
    public async Task<IActionResult> Minhas() => Ok(await portal.MinhasAsync(Autor));
    [HttpGet("minhas/{id:int}"), Authorize(Roles="Cidadao")]
    public async Task<IActionResult> Minha(int id) {var o=await portal.MinhaAsync(id,Autor);return o is null ? NotFound() : Ok(o);}
    [HttpPost("minhas"), Authorize(Roles="Cidadao")]
    public async Task<IActionResult> CriarMinha([FromBody] NovaReclamacaoDto dto) => StatusCode(201,await portal.CriarAsync(Autor,dto));
    public sealed class NovaReclamacaoComFotosForm
    {
        [Range(1, int.MaxValue)] public int IdCategoria { get; set; }
        [Required, StringLength(10000, MinimumLength = 10)] public string Descricao { get; set; } = "";
        [Required, StringLength(100)] public string Bairro { get; set; } = "";
        [Required, StringLength(200)] public string Endereco { get; set; } = "";
        public bool LocalizacaoConfirmada { get; set; }
        [Range(-90, 90)] public decimal Latitude { get; set; }
        [Range(-180, 180)] public decimal Longitude { get; set; }
        public List<IFormFile> Fotos { get; set; } = [];
    }
    [HttpPost("minhas/com-fotos"), Authorize(Roles="Cidadao"), EnableRateLimiting("photo-upload"), Consumes("multipart/form-data"), RequestSizeLimit(42 * 1024 * 1024)]
    public async Task<IActionResult> CriarMinhaComFotos([FromForm] NovaReclamacaoComFotosForm form, CancellationToken ct)
    {
        if (form.Fotos.Count > FotosOcorrenciaService.MaxFotos) return BadRequest(new { detail = $"Anexe no máximo {FotosOcorrenciaService.MaxFotos} fotografias." });
        var uploads = new List<UploadFoto>();
        foreach (var file in form.Fotos)
        {
            if (file.Length is 0 or > FotosOcorrenciaService.MaxBytes) return BadRequest(new { detail = "Cada imagem deve ter até 8 MB." });
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var expectedMime = extension switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => null };
            if (expectedMime is null || !string.Equals(file.ContentType, expectedMime, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { detail = "Formato inválido. Envie imagens JPG, PNG ou WebP com o tipo MIME correspondente." });
            await using var stream = new MemoryStream((int)file.Length);
            await file.CopyToAsync(stream, ct);
            uploads.Add(new(file.FileName, file.ContentType, stream.ToArray()));
        }
        FotosOcorrenciaService.Validar(uploads);
        var dto = new NovaReclamacaoDto { IdCategoria = form.IdCategoria, Descricao = form.Descricao, Bairro = form.Bairro, Endereco = form.Endereco, LocalizacaoConfirmada = form.LocalizacaoConfirmada, Latitude = form.Latitude, Longitude = form.Longitude };
        if (!TryValidateModel(dto)) return ValidationProblem(ModelState);
        var occurrence = await portal.CriarAsync(Autor, dto);
        try
        {
            var attachments = await fotos.SalvarAsync(occurrence.IdOcorrencia, uploads, ct);
            return StatusCode(201, new { ocorrencia = occurrence, fotos = attachments, aviso = (string?)null });
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError("Não foi possível armazenar as fotografias da reclamação {OccurrenceId}. Tipo: {ExceptionType}", occurrence.IdOcorrencia, ex.GetType().Name);
            return StatusCode(201, new { ocorrencia = occurrence, fotos = Array.Empty<FotoOcorrenciaDto>(), aviso = "A reclamação foi registrada, mas não foi possível armazenar as fotografias. Anote o protocolo e tente anexá-las novamente." });
        }
    }
    [HttpGet, Authorize(Roles="Administrador,Funcionário")]
    public async Task<ActionResult<List<OcorrenciaDto>>> Listar([FromQuery] string? status, [FromQuery] string? prioridade) =>
        Ok(await service.ListarAsync(status, prioridade));
    [HttpGet("{id:int}"), Authorize(Roles="Administrador,Funcionário,Cidadao")]
    public async Task<ActionResult<OcorrenciaDto>> Obter([FromRoute] int id)
    {
        if (User.IsInRole("Cidadao")) { var minha = await portal.MinhaAsync(id, Autor); return minha is null ? NotFound() : Ok(minha); }
        var o = await service.ObterAsync(id); return o is null ? NotFound() : Ok(o);
    }
    [HttpGet("{id:int}/fotos"), Authorize(Roles="Administrador,Funcionário,Cidadao")]
    public async Task<IActionResult> Fotos(int id, CancellationToken ct)
    {
        if (User.IsInRole("Cidadao") && await fotos.ProprietarioAsync(id, ct) != Autor) return NotFound();
        var result = await fotos.ListarAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }
    [HttpGet("{id:int}/fotos/{fotoId:int}"), Authorize(Roles="Administrador,Funcionário,Cidadao"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Foto(int id, int fotoId, CancellationToken ct)
    {
        if (User.IsInRole("Cidadao") && await fotos.ProprietarioAsync(id, ct) != Autor) return NotFound();
        var foto = await fotos.ObterAsync(id, fotoId, ct);
        return foto is null ? NotFound() : File(foto.Value.Bytes, foto.Value.Mime);
    }
    [HttpPost, Authorize(Roles="Administrador,Funcionário,Cidadao")]
    public async Task<IActionResult> Criar([FromBody] JsonElement body)
    {
        var options=new JsonSerializerOptions(JsonSerializerDefaults.Web);
        if(User.IsInRole("Cidadao"))
        {
            var entrada=body.Deserialize<NovaReclamacaoDto>(options);
            if(entrada is null)return BadRequest();
            if(!TryValidateModel(entrada))return ValidationProblem(ModelState);
            return StatusCode(201,await portal.CriarAsync(Autor,entrada));
        }
        var dto=body.Deserialize<SalvarOcorrenciaDto>(options);
        if(dto is null)return BadRequest();
        if(!TryValidateModel(dto))return ValidationProblem(ModelState);
        var o = await service.CriarAsync(dto, Autor);
        return CreatedAtAction(nameof(Obter), new { id = o.IdOcorrencia }, o);
    }
    [HttpPut("{id:int}"), Authorize(Roles="Administrador,Funcionário")]
    public async Task<ActionResult<OcorrenciaDto>> Editar([FromRoute] int id, [FromBody] SalvarOcorrenciaDto dto)
    {
        var o = await service.EditarAsync(id, dto, Autor); return o is null ? NotFound() : Ok(o);
    }
    [HttpPut("{id:int}/status"), Authorize(Roles="Administrador,Funcionário")]
    public async Task<ActionResult<OcorrenciaDto>> Status([FromRoute] int id, [FromBody] AtualizarStatusDto dto)
    {
        var o = await service.AlterarStatusAsync(id, dto.Status, Autor); return o is null ? NotFound() : Ok(o);
    }
    [HttpPost("{id:int}/historico"), Authorize(Roles="Administrador,Funcionário")]
    public async Task<IActionResult> Observar([FromRoute] int id, [FromBody] ObservacaoDto dto) =>
        await service.ObservarAsync(id, dto.Descricao, Autor) ? NoContent() : NotFound();
}
