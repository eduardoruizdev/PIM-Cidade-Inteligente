using SantosConecta.Application.DTOs;
using SantosConecta.Domain.Entities;
namespace SantosConecta.Application.Services;

public interface IRespostaService
{
    Task<SugestaoDto> GerarAsync(Ocorrencia ocorrencia, CancellationToken cancellationToken);
}
