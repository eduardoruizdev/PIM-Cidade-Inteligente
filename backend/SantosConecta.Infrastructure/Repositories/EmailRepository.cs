using Microsoft.EntityFrameworkCore;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
using SantosConecta.Infrastructure.Data;
namespace SantosConecta.Infrastructure.Repositories;

public class EmailRepository(SantosConectaDbContext db) : IEmailRepository
{
    public Task<List<EmailEnviado>> PorOcorrenciaAsync(int id) => db.Emails.AsNoTracking()
        .Include(e => e.Funcionario).Where(e => e.IdOcorrencia == id)
        .OrderByDescending(e => e.IdEmail).ToListAsync();
    public async Task AdicionarAsync(EmailEnviado e) { db.Emails.Add(e); await db.SaveChangesAsync(); }
    public async Task SalvarAsync() => await db.SaveChangesAsync();
}
