using Microsoft.EntityFrameworkCore;
using SantosConecta.Domain.Entities;
namespace SantosConecta.Infrastructure.Data;

public class SantosConectaDbContext(DbContextOptions<SantosConectaDbContext> options) : DbContext(options)
{
    public DbSet<Cidadao> Cidadaos => Set<Cidadao>();
    public DbSet<Funcionario> Funcionarios => Set<Funcionario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Ocorrencia> Ocorrencias => Set<Ocorrencia>();
    public DbSet<HistoricoOcorrencia> Historicos => Set<HistoricoOcorrencia>();
    public DbSet<EmailEnviado> Emails => Set<EmailEnviado>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Cidadao>().ToTable("Cidadao").HasKey(c => c.IdCidadao);
        model.Entity<Funcionario>().ToTable("Funcionario").HasKey(f => f.IdFuncionario);
        model.Entity<Categoria>().ToTable("Categoria").HasKey(c => c.IdCategoria);
        model.Entity<Ocorrencia>().ToTable("Ocorrencia").HasKey(o => o.IdOcorrencia);
        model.Entity<HistoricoOcorrencia>().ToTable("HistoricoOcorrencia").HasKey(h => h.IdHistorico);
        model.Entity<EmailEnviado>().ToTable("EmailEnviado").HasKey(e => e.IdEmail);
        model.Entity<Cidadao>().Property(c => c.Email).IsUnicode(false);
        model.Entity<Cidadao>().Property(c => c.Telefone).IsUnicode(false);
        model.Entity<Funcionario>().Property(f => f.Email).IsUnicode(false);
        model.Entity<Funcionario>().Property(f => f.Perfil).IsUnicode(false);
        model.Entity<Funcionario>().HasIndex(f => f.Email).IsUnique().HasDatabaseName("UQ_Funcionario_Email");
        model.Entity<Ocorrencia>().Property(o => o.Protocolo).IsUnicode(false);
        model.Entity<Ocorrencia>().Property(o => o.Status).IsUnicode(false);
        model.Entity<Ocorrencia>().Property(o => o.Prioridade).IsUnicode(false);
        model.Entity<Ocorrencia>().Property(o => o.Latitude).HasPrecision(10, 7);
        model.Entity<Ocorrencia>().Property(o => o.Longitude).HasPrecision(10, 7);
        model.Entity<Ocorrencia>().HasIndex(o => o.Protocolo).IsUnique().HasDatabaseName("UQ_Ocorrencia_Protocolo");
        model.Entity<Ocorrencia>().HasOne(o => o.Cidadao).WithMany().HasForeignKey(o => o.IdCidadao).OnDelete(DeleteBehavior.NoAction);
        model.Entity<Ocorrencia>().HasOne(o => o.Funcionario).WithMany().HasForeignKey(o => o.IdFuncionario).OnDelete(DeleteBehavior.NoAction);
        model.Entity<Ocorrencia>().HasOne(o => o.Categoria).WithMany().HasForeignKey(o => o.IdCategoria).OnDelete(DeleteBehavior.NoAction);
        model.Entity<HistoricoOcorrencia>().HasOne(h => h.Ocorrencia).WithMany(o => o.Historico).HasForeignKey(h => h.IdOcorrencia).OnDelete(DeleteBehavior.NoAction);
        model.Entity<HistoricoOcorrencia>().HasOne(h => h.Funcionario).WithMany().HasForeignKey(h => h.IdFuncionario).OnDelete(DeleteBehavior.NoAction);
        model.Entity<EmailEnviado>().HasOne(e => e.Ocorrencia).WithMany().HasForeignKey(e => e.IdOcorrencia).OnDelete(DeleteBehavior.NoAction);
        model.Entity<EmailEnviado>().HasOne(e => e.Funcionario).WithMany().HasForeignKey(e => e.IdFuncionario).OnDelete(DeleteBehavior.NoAction);
        model.Entity<EmailEnviado>().Property(e => e.Destinatario).IsUnicode(false);
        model.Entity<EmailEnviado>().Property(e => e.StatusEnvio).IsUnicode(false);
    }
}
