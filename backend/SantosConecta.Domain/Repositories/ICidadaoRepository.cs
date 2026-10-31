using SantosConecta.Domain.Entities;
namespace SantosConecta.Domain.Repositories;

public interface ICidadaoRepository
{
    Task<List<Cidadao>> ListarAsync();
    Task<Cidadao?> ObterAsync(int id);
    Task AdicionarAsync(Cidadao cidadao);
}
