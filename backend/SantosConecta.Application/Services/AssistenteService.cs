using SantosConecta.Application.DTOs;
using SantosConecta.Domain.Repositories;
namespace SantosConecta.Application.Services;

public class AssistenteService(IOcorrenciaRepository ocorrencias, IRespostaService respostas)
{
    public async Task<SugestaoDto?> AnalisarAsync(int id,string tarefa,CancellationToken ct) {
        var o=await ocorrencias.ObterAsync(id);return o is null?null:await respostas.AnalisarAsync(o,tarefa,ct);
    }
    public async Task<SugestaoDto?> GerarAsync(int id, CancellationToken cancellationToken)
    {
        var o = await ocorrencias.ObterAsync(id);
        return o is null ? null : await respostas.GerarAsync(o, cancellationToken);
    }
}
