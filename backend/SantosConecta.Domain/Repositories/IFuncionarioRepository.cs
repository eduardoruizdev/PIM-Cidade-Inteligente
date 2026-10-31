using SantosConecta.Domain.Entities;
namespace SantosConecta.Domain.Repositories;

public interface IFuncionarioRepository
{
    Task<List<Funcionario>> ListarAsync();
    Task<Funcionario?> ObterAsync(int id);
    Task<Funcionario?> PorEmailAsync(string email);
}
