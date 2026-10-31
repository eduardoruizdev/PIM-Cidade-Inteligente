using Microsoft.EntityFrameworkCore;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
using SantosConecta.Infrastructure.Data;
namespace SantosConecta.Infrastructure.Repositories;

public class CategoriaRepository(SantosConectaDbContext db) : ICategoriaRepository
{
    public Task<List<Categoria>> ListarAsync() => db.Categorias.AsNoTracking().OrderBy(c => c.Nome).ToListAsync();
    public Task<Categoria?> ObterAsync(int id) => db.Categorias.FirstOrDefaultAsync(c => c.IdCategoria == id);
}
