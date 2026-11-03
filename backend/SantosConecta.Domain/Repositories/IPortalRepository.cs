using SantosConecta.Domain.Entities;
namespace SantosConecta.Domain.Repositories;

public interface IPortalRepository
{
    Task<Cidadao?> PorEmailAsync(string email);
    Task<Cidadao?> CidadaoAsync(int id);
    Task AdicionarAsync(Cidadao cidadao);
    Task<List<Ocorrencia>> MinhasAsync(int cidadao);
    Task<Ocorrencia?> MinhaAsync(int id, int cidadao);
    Task AdicionarAsync(Ocorrencia ocorrencia);
    Task<List<EmailEnviado>> RespostasAsync(int ocorrencia);
    Task<List<IndicadorTerritorial>> IndicadoresAsync();
    Task SalvarAsync();
}
