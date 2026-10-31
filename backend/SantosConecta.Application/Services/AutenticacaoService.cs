using Microsoft.AspNetCore.Identity;
using SantosConecta.Application.DTOs;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
namespace SantosConecta.Application.Services;

public class AutenticacaoService(IFuncionarioRepository funcionarios, IPasswordHasher<Funcionario> hasher)
{
    public async Task<FuncionarioDto?> EntrarAsync(LoginDto dto)
    {
        var f = await funcionarios.PorEmailAsync(dto.Email.Trim().ToLowerInvariant());
        if (f is null) return null;
        var resultado = hasher.VerifyHashedPassword(f, f.SenhaHash, dto.Senha);
        return resultado == PasswordVerificationResult.Failed ? null : Mapeamento.Dto(f);
    }
}
