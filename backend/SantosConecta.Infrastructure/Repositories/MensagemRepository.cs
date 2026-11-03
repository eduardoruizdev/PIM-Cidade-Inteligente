using Microsoft.EntityFrameworkCore;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
using SantosConecta.Infrastructure.Data;
namespace SantosConecta.Infrastructure.Repositories;

public class MensagemRepository(SantosConectaDbContext db) : IMensagemRepository
{
    public Task<int> PendentesAsync(DateTime? inicio, DateTime? fim, string? status, string? prioridade) => db.Ocorrencias.CountAsync(o => (status == null || status == "" || o.Status == status) && (prioridade == null || prioridade == "" || o.Prioridade == prioridade) && (!inicio.HasValue || o.DataAbertura >= inicio) && (!fim.HasValue || o.DataAbertura < fim) && db.Mensagens.Any(m => m.IdOcorrencia == o.IdOcorrencia && m.TipoRemetente == "Cidadao" && !db.Mensagens.Any(r => r.IdOcorrencia == o.IdOcorrencia && r.TipoRemetente == "Equipe" && r.DataEnvio > m.DataEnvio)));
    private IQueryable<MensagemOcorrencia> Destinadas(int usuario, string tipo, bool administrador) => db.Mensagens.AsNoTracking().Where(m => tipo == "Cidadao"
        ? m.TipoRemetente == "Equipe" && m.Ocorrencia.IdCidadao == usuario
        : m.TipoRemetente == "Cidadao" && (administrador || m.Ocorrencia.IdFuncionario == usuario || m.Ocorrencia.IdFuncionario == null));
    private IQueryable<MensagemOcorrencia> NaoLidas(int usuario, string tipo, bool administrador) => Destinadas(usuario, tipo, administrador).Where(m => tipo == "Cidadao" ? m.DataLeitura == null : !db.LeiturasMensagens.Any(l => l.IdMensagem == m.IdMensagem && l.IdFuncionario == usuario));
    public Task<int> QuantidadeAsync(int usuario, string tipo, bool administrador) => NaoLidas(usuario, tipo, administrador).CountAsync();
    public Task<List<NotificacaoMensagem>> NotificacoesAsync(int usuario, string tipo, bool administrador) => Destinadas(usuario, tipo, administrador)
        .OrderBy(m => tipo == "Cidadao" ? m.DataLeitura != null : db.LeiturasMensagens.Any(l => l.IdMensagem == m.IdMensagem && l.IdFuncionario == usuario))
        .ThenByDescending(m => m.DataEnvio).ThenByDescending(m => m.IdMensagem).Take(100)
        .Select(m => new NotificacaoMensagem(m.IdMensagem, m.IdOcorrencia, m.Ocorrencia.Protocolo, m.Ocorrencia.Categoria.Nome, m.NomeRemetente, m.Conteudo, m.DataEnvio,
            tipo == "Cidadao" ? m.DataLeitura : db.LeiturasMensagens.Where(l => l.IdMensagem == m.IdMensagem && l.IdFuncionario == usuario).Select(l => (DateTime?)l.DataLeitura).FirstOrDefault())).ToListAsync();
    public async Task<bool> MarcarNotificacaoAsync(int mensagem, int usuario, string tipo, bool administrador)
    {
        if (!await Destinadas(usuario, tipo, administrador).AnyAsync(m => m.IdMensagem == mensagem)) return false;
        if (tipo == "Cidadao") await db.Mensagens.Where(m => m.IdMensagem == mensagem && m.DataLeitura == null).ExecuteUpdateAsync(s => s.SetProperty(m => m.DataLeitura, DateTime.UtcNow));
        else
        {
            // Inserção idempotente e serializada por chave: duas abas não criam duas leituras.
            await db.Database.ExecuteSqlInterpolatedAsync($@"INSERT INTO LeituraMensagem (IdMensagem, IdFuncionario, DataLeitura)
SELECT {mensagem}, {usuario}, SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM LeituraMensagem WITH (UPDLOCK, HOLDLOCK) WHERE IdMensagem = {mensagem} AND IdFuncionario = {usuario})");
        }
        return true;
    }
    public Task<bool> PodeAcessarAsync(int id, int? cidadao) => db.Ocorrencias.AnyAsync(o => o.IdOcorrencia == id && (!cidadao.HasValue || o.IdCidadao == cidadao));
    public async Task<List<MensagemOcorrencia>> ListarAsync(int id, int usuario, string tipo)
    {
        var messages = await db.Mensagens.AsNoTracking().Where(m => m.IdOcorrencia == id).OrderBy(m => m.DataEnvio).ThenBy(m => m.IdMensagem).ToListAsync();
        if (tipo == "Equipe")
        {
            var reads = await db.LeiturasMensagens.Where(l => l.IdFuncionario == usuario && l.Mensagem.IdOcorrencia == id).ToDictionaryAsync(l => l.IdMensagem, l => l.DataLeitura);
            foreach (var m in messages.Where(m => m.TipoRemetente == "Cidadao")) m.DataLeitura = reads.TryGetValue(m.IdMensagem, out var date) ? date : null;
        }
        return messages;
    }
    public async Task AdicionarAsync(MensagemOcorrencia m) { db.Mensagens.Add(m); await db.SaveChangesAsync(); }
    public async Task MarcarLidasAsync(int id, string tipo, int usuario, bool administrador)
    {
        var ids = await NaoLidas(usuario, tipo, administrador).Where(m => m.IdOcorrencia == id).Select(m => m.IdMensagem).ToListAsync();
        foreach (var message in ids) await MarcarNotificacaoAsync(message, usuario, tipo, administrador);
    }
}
