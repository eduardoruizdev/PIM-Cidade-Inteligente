using SantosConecta.Application.DTOs;
using SantosConecta.Domain.Entities;
using SantosConecta.Domain.Repositories;
namespace SantosConecta.Application.Services;

public class OcorrenciaService(IOcorrenciaRepository ocorrencias, ICidadaoRepository cidadaos,
    IFuncionarioRepository funcionarios, ICategoriaRepository categorias)
{
    public async Task<List<OcorrenciaDto>> ListarAsync(string? status, string? prioridade) =>
        (await ocorrencias.ListarAsync(status, prioridade)).Select(Mapeamento.Dto).ToList();
    public async Task<OcorrenciaDto?> ObterAsync(int id)
    {
        var ocorrencia = await ocorrencias.ObterAsync(id);
        return ocorrencia is null ? null : Mapeamento.Dto(ocorrencia);
    }
    private async Task ValidarRelacionamentosAsync(SalvarOcorrenciaDto dto)
    {
        if (await cidadaos.ObterAsync(dto.IdCidadao) is null)
            throw new ArgumentException("Cidadão não encontrado.");
        if (await categorias.ObterAsync(dto.IdCategoria) is null)
            throw new ArgumentException("Categoria não encontrada.");
        if (dto.IdFuncionario.HasValue && (await funcionarios.ObterAsync(dto.IdFuncionario.Value))?.Ativo != true)
            throw new ArgumentException("Funcionário não encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Titulo) || string.IsNullOrWhiteSpace(dto.Descricao) ||
            string.IsNullOrWhiteSpace(dto.Bairro) || string.IsNullOrWhiteSpace(dto.Endereco))
            throw new ArgumentException("Preencha os campos obrigatórios da reclamação.");
    }
    private static void Preencher(Ocorrencia o, SalvarOcorrenciaDto dto)
    {
        o.Titulo = dto.Titulo.Trim(); o.Descricao = dto.Descricao.Trim();
        o.Bairro = dto.Bairro.Trim(); o.Endereco = dto.Endereco.Trim();
        o.Latitude = dto.Latitude; o.Longitude = dto.Longitude;
        if(dto.LocalizacaoConfirmada.HasValue)o.LocalizacaoConfirmada=dto.LocalizacaoConfirmada.Value;
        o.Status = dto.Status; o.Prioridade = dto.Prioridade;
        o.IdCidadao = dto.IdCidadao; o.IdCategoria = dto.IdCategoria;
        o.IdFuncionario = dto.IdFuncionario;
    }
    private static void Registrar(Ocorrencia o, int funcionario, string descricao) =>
        o.Historico.Add(new HistoricoOcorrencia
        {
            IdFuncionario = funcionario,
            Descricao = descricao,
            DataAlteracao = DateTime.UtcNow
        });
    public async Task<OcorrenciaDto> CriarAsync(SalvarOcorrenciaDto dto, int autor)
    {
        await ValidarRelacionamentosAsync(dto);
        var o = new Ocorrencia
        {
            DataAbertura = DateTime.UtcNow,
            Protocolo = $"{DateTime.UtcNow:yyyy}-{Guid.NewGuid():N}"[..25]
        };
        Preencher(o, dto);
        Registrar(o, autor, $"Reclamação cadastrada. Status inicial: {o.Status}.");
        await ocorrencias.AdicionarAsync(o);
        return (await ObterAsync(o.IdOcorrencia))!;
    }
    public async Task<OcorrenciaDto?> EditarAsync(int id, SalvarOcorrenciaDto dto, int autor)
    {
        var o = await ocorrencias.ObterAsync(id);
        if (o is null) return null;
        await ValidarRelacionamentosAsync(dto);
        var statusAnterior = o.Status;
        var prioridadeAnterior = o.Prioridade;
        if(prioridadeAnterior!=dto.Prioridade && (dto.JustificativaPrioridade?.Trim().Length??0)<10)
            throw new ArgumentException("Justifique a alteração de prioridade com pelo menos 10 caracteres.");
        var funcionarioAnterior = o.IdFuncionario;
        Preencher(o, dto);
        if(prioridadeAnterior!=o.Prioridade)
            Registrar(o,autor,$"Prioridade administrativa: {prioridadeAnterior} → {o.Prioridade}. {dto.JustificativaPrioridade!.Trim()}");
        Registrar(o, autor, $"Dados atualizados. Status: {statusAnterior} → {o.Status}. Responsável: {funcionarioAnterior?.ToString() ?? "não atribuído"} → {o.IdFuncionario?.ToString() ?? "não atribuído"}.");
        if (statusAnterior != o.Status) Registrar(o, autor, $"Status alterado para {o.Status}.");
        await ocorrencias.SalvarAsync();
        return (await ObterAsync(id))!;
    }
    public async Task<OcorrenciaDto?> AlterarStatusAsync(int id, string status, int autor)
    {
        var o = await ocorrencias.ObterAsync(id);
        if (o is null) return null;
        o.Status = status;
        Registrar(o, autor, $"Status alterado para {status}.");
        await ocorrencias.SalvarAsync();
        return (await ObterAsync(id))!;
    }
    public async Task<bool> ObservarAsync(int id, string texto, int autor)
    {
        var o = await ocorrencias.ObterAsync(id);
        if (o is null) return false;
        if (string.IsNullOrWhiteSpace(texto)) throw new ArgumentException("Informe a observação.");
        Registrar(o, autor, texto.Trim());
        await ocorrencias.SalvarAsync();
        return true;
    }
}
