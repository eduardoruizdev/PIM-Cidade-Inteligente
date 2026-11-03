using Microsoft.Extensions.Logging;

namespace SantosConecta.Infrastructure.Services;

public record SentidoTravessia(string Sentido, int? EsperaMinutos, string? SituacaoOperacional,
    DateTimeOffset? InformadoEm, DateTimeOffset? ConsultadoEm, bool Disponivel, bool Desatualizado);
public record DadosTravessia(string Modo, string Fonte, string FonteUrl, string Aviso,
    int AtualizacaoSegundos, int? Embarcacoes, IReadOnlyList<string> Comunicados,
    IReadOnlyList<SentidoTravessia> Sentidos, ConsultaExternaTravessia? ConsultaExterna = null);
public record ConsultaExternaTravessia(string? Url, string? Nome, bool Disponivel,
    DateTimeOffset? VerificadoEm, string Mensagem);

/// <summary>
/// Consulta assistida: nenhuma coleta externa está autorizada/configurada.
/// Horários nulos significam ausência de medição, nunca horário da requisição à API.
/// Substitua este serviço quando houver contrato público e permissão verificados.
/// </summary>
public class TravessiaService(HttpClient? http = null, ILogger<TravessiaService>? logger = null,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim linkLock = new(1, 1);
    private ConsultaExternaTravessia? link;
    private DateTimeOffset verificarApos;
    public const string FonteUrl = "https://semil.sp.gov.br/travessias/travessias-automoveis/santos-guaruja/";
    private static readonly DadosTravessia Indisponivel = new(
        "consulta_oficial", "SEMIL — Departamento Hidroviário de São Paulo", FonteUrl,
        "Leitura automática indisponível: não há fonte de integração com acesso e atualização verificados. " +
        "Os tempos de espera não puderam ser confirmados. Tente novamente antes de viajar.",
        300, null, Array.Empty<string>(),
        Array.AsReadOnly(new[] {
            new SentidoTravessia("Santos → Guarujá", null, null, null, null, false, false),
            new SentidoTravessia("Guarujá → Santos", null, null, null, null, false, false)
        }));

    public DadosTravessia Consultar() => Indisponivel;
    public async Task<DadosTravessia> ConsultarAsync(CancellationToken ct = default)
        => Indisponivel with { ConsultaExterna = await VerificarLinkAsync(ct) };

    // Verifica somente acessibilidade. Não extrai nem republica tempos de espera.
    // Fontes fixas, TLS normal, sem seguir redirecionamentos para hosts arbitrários.
    private async Task<ConsultaExternaTravessia> VerificarLinkAsync(CancellationToken ct)
    {
        if (http is null) return new(null, null, false, null, "Consulta externa não verificada.");
        await linkLock.WaitAsync(ct);
        try
        {
            if (link is not null && clock.GetUtcNow() < verificarApos) return link;
            var candidates = new[] {
                (Url: FonteUrl, Nome: "SEMIL · canal oficial"),
                (Url: "https://metroa.com.br/santos/balsas/santos-guaruja", Nome: "METROA · fonte independente")
            };
            foreach (var candidate in candidates)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                try
                {
                    var uri = new Uri(candidate.Url);
                    for (var hop = 0; hop < 4; hop++)
                    {
                        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                        if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is {} location)
                        {
                            var next = new Uri(uri, location);
                            if (next.Scheme != "https" || next.Host != new Uri(candidate.Url).Host) break;
                            uri = next;
                            continue;
                        }
                        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType != "text/html")
                        {
                            logger?.LogWarning("Consulta externa da travessia: {Host} respondeu HTTP {Status}", uri.Host, (int)response.StatusCode);
                            break;
                        }
                        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                        using var reader = new StreamReader(stream);
                        var buffer = new char[32768];
                        var count = await reader.ReadBlockAsync(buffer.AsMemory(), timeout.Token);
                        var html = new string(buffer, 0, count);
                        if (!html.Contains("santos", StringComparison.OrdinalIgnoreCase) ||
                            !html.Contains("guaruj", StringComparison.OrdinalIgnoreCase) ||
                            html.Contains("Just a moment", StringComparison.OrdinalIgnoreCase) ||
                            html.Contains("Access denied", StringComparison.OrdinalIgnoreCase))
                        {
                            logger?.LogWarning("Página de consulta da travessia sem conteúdo verificável: {Host}", uri.Host);
                            break;
                        }
                        link = new(uri.AbsoluteUri, candidate.Nome, true, clock.GetUtcNow(),
                            "Página acessível na última verificação. Confira a data e os avisos publicados pela fonte.");
                        verificarApos = clock.GetUtcNow().AddMinutes(30);
                        return link;
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException)
                {
                    ct.ThrowIfCancellationRequested();
                    logger?.LogWarning("Falha ao verificar página da travessia: {Host}, {Tipo}, {Erro}",
                        new Uri(candidate.Url).Host, ex.GetType().Name,
                        ex is HttpRequestException requestError ? requestError.HttpRequestError.ToString() : "TimeoutOuLeitura");
                }
            }
            link = new(null, null, false, clock.GetUtcNow(),
                "Nenhuma página de consulta pôde ser validada agora. Tente novamente mais tarde.");
            verificarApos = clock.GetUtcNow().AddMinutes(30);
            return link;
        }
        finally { linkLock.Release(); }
    }
    public object Historico(string periodo)
    {
        if (periodo is not ("horas" or "hoje" or "7dias"))
            throw new ArgumentException("Período inválido. Use horas, hoje ou 7dias.");
        return new { periodo, itens = Array.Empty<object>(), suficiente = false,
            aviso = "Ainda não há medições verificadas para apresentar histórico de espera." };
    }
}
