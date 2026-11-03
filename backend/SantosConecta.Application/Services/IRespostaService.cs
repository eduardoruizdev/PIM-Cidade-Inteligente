using SantosConecta.Application.DTOs;
using SantosConecta.Domain.Entities;
namespace SantosConecta.Application.Services;

public interface IRespostaService
{
    Task<SugestaoDto> AnalisarAsync(Ocorrencia ocorrencia, string tarefa, CancellationToken cancellationToken);
    Task<SugestaoDto> GerarAsync(Ocorrencia ocorrencia, CancellationToken cancellationToken);
    Task<ClassificacaoDto> ClassificarAsync(string descricao, List<CategoriaDto> categorias, CancellationToken cancellationToken);
}
