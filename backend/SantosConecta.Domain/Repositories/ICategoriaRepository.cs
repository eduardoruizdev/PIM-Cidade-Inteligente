using SantosConecta.Domain.Entities;
namespace SantosConecta.Domain.Repositories;

public interface ICategoriaRepository
{
    Task<List<Categoria>> ListarAsync();
    Task<Categoria?> ObterAsync(int id);
}
