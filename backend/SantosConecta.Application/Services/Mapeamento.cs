using SantosConecta.Application.DTOs;
using SantosConecta.Domain.Entities;
namespace SantosConecta.Application.Services;

public static class Mapeamento
{
    private static DateTime Utc(DateTime date) => DateTime.SpecifyKind(date, DateTimeKind.Utc);
    public static CidadaoDto Dto(Cidadao c) => new(c.IdCidadao, c.Nome, c.Email, c.Telefone);
    // Nunca expor SenhaHash nos endpoints ou no Swagger.
    public static FuncionarioDto Dto(Funcionario f) => new(f.IdFuncionario, f.Nome, f.Email, f.Perfil);
    public static CategoriaDto Dto(Categoria c) => new(c.IdCategoria, c.Nome);
    public static OcorrenciaDto Dto(Ocorrencia o) => new(o.IdOcorrencia, o.Protocolo,
        o.Titulo, o.Descricao, o.Bairro, o.Endereco, o.Latitude, o.Longitude, Utc(o.DataAbertura),
        o.Status, o.Prioridade, Dto(o.Cidadao), o.Funcionario is null ? null : Dto(o.Funcionario),
        Dto(o.Categoria), o.Historico.OrderBy(h => h.DataAlteracao).Select(h =>
            new HistoricoDto(h.IdHistorico, h.Descricao, Utc(h.DataAlteracao), h.Funcionario.Nome)).ToList());
    public static EmailDto Dto(EmailEnviado e) => new(e.IdEmail, e.IdOcorrencia, e.Destinatario,
        e.Assunto, e.Mensagem, e.DataEnvio is null ? null : Utc(e.DataEnvio.Value), e.StatusEnvio, e.Erro, e.Funcionario.Nome);
}
