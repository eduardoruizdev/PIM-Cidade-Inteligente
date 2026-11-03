using Microsoft.EntityFrameworkCore;
using SantosConecta.Domain.Entities;
namespace SantosConecta.Infrastructure.Data;

public class SantosConectaDbContext(DbContextOptions<SantosConectaDbContext> options) : DbContext(options)
{
    public DbSet<AlertaMeteorologico> AlertasMeteorologicos => Set<AlertaMeteorologico>();
    public DbSet<PreferenciaMeteorologica> PreferenciasMeteorologicas => Set<PreferenciaMeteorologica>();
    public DbSet<EntregaMeteorologica> EntregasMeteorologicas => Set<EntregaMeteorologica>();
    public DbSet<LeituraMensagem> LeiturasMensagens => Set<LeituraMensagem>();
    public DbSet<MensagemOcorrencia> Mensagens => Set<MensagemOcorrencia>();
    public DbSet<Cidadao> Cidadaos => Set<Cidadao>();
    public DbSet<IndicadorTerritorial> IndicadoresTerritoriais => Set<IndicadorTerritorial>();
    public DbSet<Funcionario> Funcionarios => Set<Funcionario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Ocorrencia> Ocorrencias => Set<Ocorrencia>();
    public DbSet<HistoricoOcorrencia> Historicos => Set<HistoricoOcorrencia>();
    public DbSet<EmailEnviado> Emails => Set<EmailEnviado>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<AlertaMeteorologico>().ToTable("AlertaMeteorologico").HasKey(a=>a.Id);
        model.Entity<PreferenciaMeteorologica>().ToTable("PreferenciaMeteorologica").HasKey(p=>p.IdCidadao);
        model.Entity<EntregaMeteorologica>().ToTable("EntregaMeteorologica").HasKey(e=>new {e.IdAlerta,e.IdCidadao});
        model.Entity<LeituraMensagem>().ToTable("LeituraMensagem").HasKey(l => new { l.IdMensagem, l.IdFuncionario });
        model.Entity<LeituraMensagem>().HasOne(l => l.Mensagem).WithMany().HasForeignKey(l => l.IdMensagem).OnDelete(DeleteBehavior.NoAction);
        model.Entity<LeituraMensagem>().HasOne(l => l.Funcionario).WithMany().HasForeignKey(l => l.IdFuncionario).OnDelete(DeleteBehavior.NoAction);
        model.Entity<MensagemOcorrencia>().ToTable("MensagemOcorrencia").HasKey(m=>m.IdMensagem);
        model.Entity<MensagemOcorrencia>().HasOne(m=>m.Ocorrencia).WithMany().HasForeignKey(m=>m.IdOcorrencia).OnDelete(DeleteBehavior.NoAction);
        model.Entity<MensagemOcorrencia>().HasIndex(m=>new {m.IdOcorrencia,m.DataEnvio});
        model.Entity<Cidadao>().ToTable("Cidadao").HasKey(c => c.IdCidadao);
        model.Entity<IndicadorTerritorial>().ToTable("IndicadorTerritorial").HasKey(i => i.IdIndicador);
        model.Entity<IndicadorTerritorial>().HasIndex(i => i.Bairro).IsUnique();
        model.Entity<Cidadao>().HasIndex(c => c.Email).IsUnique().HasFilter("[SenhaHash] IS NOT NULL");
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
