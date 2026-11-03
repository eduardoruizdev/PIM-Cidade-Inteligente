using Microsoft.EntityFrameworkCore;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
using SantosConecta.Infrastructure.Data;
namespace SantosConecta.Infrastructure.Repositories;

public class FuncionarioRepository(SantosConectaDbContext db) : IFuncionarioRepository
{
    public async Task AdicionarAsync(Funcionario f) { db.Funcionarios.Add(f); await db.SaveChangesAsync(); }
    public async Task SalvarAsync() => await db.SaveChangesAsync();
    public Task<List<Funcionario>> ListarAsync() => db.Funcionarios.AsNoTracking().OrderBy(f => f.Nome).ToListAsync();
    public Task<Funcionario?> ObterAsync(int id) => db.Funcionarios.FirstOrDefaultAsync(f => f.IdFuncionario == id);
    public Task<Funcionario?> PorEmailAsync(string email) => db.Funcionarios.FirstOrDefaultAsync(f => f.Email == email);
}
