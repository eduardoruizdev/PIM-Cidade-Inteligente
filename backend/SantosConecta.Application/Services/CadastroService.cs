using SantosConecta.Application.DTOs;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
namespace SantosConecta.Application.Services;

public class CadastroService(ICidadaoRepository cidadaos, IFuncionarioRepository funcionarios,
    ICategoriaRepository categorias)
{
    public async Task<List<CidadaoDto>> CidadaosAsync() =>
        (await cidadaos.ListarAsync()).Select(Mapeamento.Dto).ToList();
    public async Task<CidadaoDto?> CidadaoAsync(int id)
    {
        var c = await cidadaos.ObterAsync(id); return c is null ? null : Mapeamento.Dto(c);
    }
    public async Task<CidadaoDto> CriarCidadaoAsync(CriarCidadaoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Informe o nome.");
        var c = new Cidadao
        {
            Nome = dto.Nome.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Telefone = dto.Telefone.Trim()
        };
        await cidadaos.AdicionarAsync(c); return Mapeamento.Dto(c);
    }
    public async Task<List<FuncionarioDto>> FuncionariosAsync() =>
        (await funcionarios.ListarAsync()).Select(Mapeamento.Dto).ToList();
    public async Task<FuncionarioDto?> FuncionarioAsync(int id)
    {
        var f = await funcionarios.ObterAsync(id); return f is null ? null : Mapeamento.Dto(f);
    }
    public async Task<List<CategoriaDto>> CategoriasAsync() =>
        (await categorias.ListarAsync()).Select(Mapeamento.Dto).ToList();
}
