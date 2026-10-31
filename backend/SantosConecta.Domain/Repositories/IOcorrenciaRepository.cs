using SantosConecta.Domain.Entities;
namespace SantosConecta.Domain.Repositories;

public interface IOcorrenciaRepository
{
    Task<List<Ocorrencia>> ListarAsync(string? status = null, string? prioridade = null);
    Task<Ocorrencia?> ObterAsync(int id);
    Task AdicionarAsync(Ocorrencia ocorrencia);
    Task SalvarAsync();
}
