using Microsoft.EntityFrameworkCore;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
using SantosConecta.Infrastructure.Data;
namespace SantosConecta.Infrastructure.Repositories;

public class PortalRepository(SantosConectaDbContext db) : IPortalRepository
{
    private IQueryable<Ocorrencia> Consulta => db.Ocorrencias.Include(o=>o.Categoria).Include(o=>o.Historico);
    public Task<Cidadao?> PorEmailAsync(string email) => db.Cidadaos.FirstOrDefaultAsync(c=>c.Email==email);
    public Task<Cidadao?> CidadaoAsync(int id) => db.Cidadaos.FirstOrDefaultAsync(c=>c.IdCidadao==id);
    public async Task AdicionarAsync(Cidadao c) { db.Cidadaos.Add(c); await db.SaveChangesAsync(); }
    public Task<List<Ocorrencia>> MinhasAsync(int cidadao) => Consulta.AsNoTracking().Where(o=>o.IdCidadao==cidadao).OrderByDescending(o=>o.DataAbertura).ToListAsync();
    public Task<Ocorrencia?> MinhaAsync(int id,int cidadao) => Consulta.AsNoTracking().FirstOrDefaultAsync(o=>o.IdOcorrencia==id && o.IdCidadao==cidadao);
    public async Task AdicionarAsync(Ocorrencia o) { db.Ocorrencias.Add(o); await db.SaveChangesAsync(); }
    public Task<List<EmailEnviado>> RespostasAsync(int id) => db.Emails.AsNoTracking().Where(e=>e.IdOcorrencia==id && e.StatusEnvio=="Enviado").OrderBy(e=>e.DataEnvio).ToListAsync();
    public Task<List<IndicadorTerritorial>> IndicadoresAsync() => db.IndicadoresTerritoriais.AsNoTracking().OrderBy(i=>i.Bairro).ToListAsync();
    public async Task SalvarAsync() => await db.SaveChangesAsync();
}
