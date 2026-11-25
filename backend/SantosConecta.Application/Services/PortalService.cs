using Microsoft.AspNetCore.Identity;
using SantosConecta.Application.DTOs;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
namespace SantosConecta.Application.Services;

public record ReclamacaoPublicaDto(int IdOcorrencia, string Protocolo, string Descricao, string Bairro, string Endereco, bool LocalizacaoConfirmada, decimal Latitude, decimal Longitude, DateTime DataAbertura, string Status, string Prioridade, string Categoria, IEnumerable<object> Historico);

public class PortalService(IPortalRepository portal, ICategoriaRepository categorias, IPasswordHasher<Cidadao> hasher)
{
    public async Task<CidadaoDto> CadastrarAsync(CadastroCidadaoDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await portal.PorEmailAsync(email) is not null) throw new ArgumentException("Não foi possível cadastrar este e-mail. Entre em contato com a equipe se já houver registro.");
        if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Telefone)) throw new ArgumentException("Informe nome e telefone.");
        if(!System.Text.RegularExpressions.Regex.IsMatch(dto.Telefone,@"^\+?[0-9 ()-]{10,20}$"))throw new ArgumentException("Informe um telefone válido.");
        var c = new Cidadao { Nome=dto.Nome.Trim(), Email=email, Telefone=dto.Telefone.Trim() };
        c.SenhaHash=hasher.HashPassword(c,dto.Senha);
        await portal.AdicionarAsync(c);
        return Mapeamento.Dto(c);
    }
    public async Task<CidadaoDto?> LoginAsync(LoginDto dto)
    {
        var c=await portal.PorEmailAsync(dto.Email.Trim().ToLowerInvariant());
        if(c?.SenhaHash is null) return null;
        PasswordVerificationResult result;
        try { result=hasher.VerifyHashedPassword(c,c.SenhaHash,dto.Senha); }
        catch (FormatException) { return null; }
        if(result==PasswordVerificationResult.Failed) return null;
        if(result==PasswordVerificationResult.SuccessRehashNeeded) { c.SenhaHash=hasher.HashPassword(c,dto.Senha); await portal.SalvarAsync(); }
        return Mapeamento.Dto(c);
    }
    public async Task<CidadaoDto?> PerfilAsync(int id) { var c=await portal.CidadaoAsync(id); return c is null ? null : Mapeamento.Dto(c); }
    public async Task<CidadaoDto?> EditarAsync(int id,PerfilCidadaoDto dto)
    {
        var c=await portal.CidadaoAsync(id); if(c is null)return null;
        if(string.IsNullOrWhiteSpace(dto.Nome))throw new ArgumentException("Informe o nome.");
        c.Nome=dto.Nome.Trim(); c.Telefone=dto.Telefone.Trim(); await portal.SalvarAsync(); return Mapeamento.Dto(c);
    }
    // DTO específico: nunca expõe outros cidadãos, funcionários ou observações internas.
    private static ReclamacaoPublicaDto Publico(Ocorrencia o) => new(o.IdOcorrencia,o.Protocolo,o.Descricao,o.Bairro,o.Endereco,o.LocalizacaoConfirmada,o.Latitude,o.Longitude,
        DateTime.SpecifyKind(o.DataAbertura,DateTimeKind.Utc),o.Status,o.Prioridade,o.Categoria.Nome,
        o.Historico.Where(h=>h.IdFuncionario==null || h.Descricao.StartsWith("Status alterado para "))
            .OrderBy(h=>h.DataAlteracao).Select(h=>(object)new {h.Descricao,DataAlteracao=DateTime.SpecifyKind(h.DataAlteracao,DateTimeKind.Utc)}));
    public async Task<List<ReclamacaoPublicaDto>> MinhasAsync(int id) => (await portal.MinhasAsync(id)).Select(Publico).ToList();
    public async Task<object?> MinhaAsync(int id,int cidadao)
    {
        var o=await portal.MinhaAsync(id,cidadao); if(o is null)return null;
        return new { Ocorrencia=Publico(o), Respostas=(await portal.RespostasAsync(id)).Select(e=>new {e.Assunto,e.Mensagem,e.DataEnvio}) };
    }
    public async Task<ReclamacaoPublicaDto> CriarAsync(int id,NovaReclamacaoDto dto)
    {
        var categoria=await categorias.ObterAsync(dto.IdCategoria) ?? throw new ArgumentException("Categoria inválida.");
        if(string.IsNullOrWhiteSpace(dto.Descricao)||string.IsNullOrWhiteSpace(dto.Bairro)||string.IsNullOrWhiteSpace(dto.Endereco))throw new ArgumentException("Preencha descrição e localização.");
        var o=new Ocorrencia { IdCidadao=id,IdCategoria=dto.IdCategoria,Titulo=categoria.Nome,Descricao=dto.Descricao.Trim(),Bairro=dto.Bairro.Trim(),Endereco=dto.Endereco.Trim(),LocalizacaoConfirmada=dto.LocalizacaoConfirmada,Latitude=dto.LocalizacaoConfirmada?dto.Latitude:0,Longitude=dto.LocalizacaoConfirmada?dto.Longitude:0,DataAbertura=DateTime.UtcNow,Protocolo=$"{DateTime.UtcNow:yyyy}-{Guid.NewGuid():N}"[..25],Status="Aberta",Prioridade="Média" };
        o.Historico.Add(new HistoricoOcorrencia { Descricao="Reclamação registrada pelo cidadão. Status: Aberta.",DataAlteracao=DateTime.UtcNow });
        await portal.AdicionarAsync(o); o.Categoria=categoria; return Publico(o);
    }
}
