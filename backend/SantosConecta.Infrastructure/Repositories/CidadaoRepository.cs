using Microsoft.EntityFrameworkCore;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
using SantosConecta.Infrastructure.Data;
namespace SantosConecta.Infrastructure.Repositories;

public class CidadaoRepository(SantosConectaDbContext db) : ICidadaoRepository
{
    public Task<List<Cidadao>> ListarAsync() => db.Cidadaos.AsNoTracking().OrderBy(c => c.Nome).ToListAsync();
    public Task<Cidadao?> ObterAsync(int id) => db.Cidadaos.FirstOrDefaultAsync(c => c.IdCidadao == id);
    public async Task AdicionarAsync(Cidadao c) { db.Cidadaos.Add(c); await db.SaveChangesAsync(); }
}
