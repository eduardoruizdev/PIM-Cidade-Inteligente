using Microsoft.EntityFrameworkCore;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
using SantosConecta.Infrastructure.Data;
namespace SantosConecta.Infrastructure.Repositories;

public class OcorrenciaRepository(SantosConectaDbContext db) : IOcorrenciaRepository
{
    private IQueryable<Ocorrencia> ComRelacionamentos() => db.Ocorrencias
        .Include(o => o.Cidadao).Include(o => o.Categoria).Include(o => o.Funcionario)
        .Include(o => o.Historico).ThenInclude(h => h.Funcionario);
    public async Task<List<Ocorrencia>> ListarAsync(string? status = null, string? prioridade = null)
    {
        var query = ComRelacionamentos().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);
        if (!string.IsNullOrWhiteSpace(prioridade)) query = query.Where(o => o.Prioridade == prioridade);
        return await query.OrderByDescending(o => o.DataAbertura).ToListAsync();
    }
    public Task<Ocorrencia?> ObterAsync(int id) =>
        ComRelacionamentos().FirstOrDefaultAsync(o => o.IdOcorrencia == id);
    public async Task AdicionarAsync(Ocorrencia o)
    {
        db.Ocorrencias.Add(o); await db.SaveChangesAsync();
    }
    public async Task SalvarAsync() => await db.SaveChangesAsync();
}
