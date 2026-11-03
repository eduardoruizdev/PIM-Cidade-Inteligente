using Microsoft.AspNetCore.Identity;
using SantosConecta.Application.DTOs;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
namespace SantosConecta.Application.Services;

public class EquipeService(IFuncionarioRepository repo, IPasswordHasher<Funcionario> hasher)
{
    public async Task<FuncionarioDto?> SalvarAsync(int? id, SalvarFuncionarioDto dto, int autor)
    {
        var f = id.HasValue ? await repo.ObterAsync(id.Value) : new Funcionario();
        if (f is null) return null;
        if (id == autor && (!dto.Ativo || dto.Perfil != "Administrador")) throw new ArgumentException("Você não pode desativar sua própria conta ou remover seu acesso administrativo.");
        var email = dto.Email.Trim().ToLowerInvariant(); var outro = await repo.PorEmailAsync(email);
        if (outro is not null && outro.IdFuncionario != f.IdFuncionario) throw new ArgumentException("E-mail já cadastrado.");
        if (!id.HasValue && string.IsNullOrWhiteSpace(dto.Senha)) throw new ArgumentException("Informe a senha inicial.");
        if (dto.Senha is not null && (!dto.Senha.Any(char.IsUpper) || !dto.Senha.Any(char.IsLower) || !dto.Senha.Any(char.IsDigit) || !dto.Senha.Any(c => !char.IsLetterOrDigit(c)))) throw new ArgumentException("Use senha com maiúsculas, minúsculas, números e símbolos.");
        f.Nome = dto.Nome.Trim(); f.Email = email; f.Telefone = dto.Telefone.Trim(); f.Cargo = dto.Cargo.Trim(); f.Perfil = dto.Perfil; f.Ativo = dto.Ativo;
        if (dto.Senha is not null) f.SenhaHash = hasher.HashPassword(f, dto.Senha);
        if (id.HasValue) await repo.SalvarAsync(); else await repo.AdicionarAsync(f);
        return Mapeamento.Dto(f);
    }
}