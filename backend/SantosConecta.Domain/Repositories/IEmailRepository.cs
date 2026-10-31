using SantosConecta.Domain.Entities;
namespace SantosConecta.Domain.Repositories;

public interface IEmailRepository
{
    Task<List<EmailEnviado>> PorOcorrenciaAsync(int id);
    Task AdicionarAsync(EmailEnviado email);
    Task SalvarAsync();
}
