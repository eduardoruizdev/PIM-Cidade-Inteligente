using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SantosConecta.Infrastructure.Services;

public record SentidoTravessia(string Sentido, int? EsperaMinutos, string? SituacaoOperacional,
    DateTimeOffset? InformadoEm, DateTimeOffset? ConsultadoEm, bool Disponivel, bool Desatualizado);
public record DadosTravessia(string Modo, string Fonte, string FonteUrl, string Aviso,
    int AtualizacaoSegundos, int? Embarcacoes, IReadOnlyList<string> Comunicados,
    IReadOnlyList<SentidoTravessia> Sentidos, ConsultaExternaTravessia? ConsultaExterna = null,
    DateTimeOffset? CapturadoEm = null);
public record ConsultaExternaTravessia(string? Url, string? Nome, bool Disponivel,
    DateTimeOffset? VerificadoEm, string Mensagem);

/// <summary>Leitura do HTML publicado pela AcquaVias, com cache da última resposta válida.</summary>
public sealed class TravessiaService(HttpClient? http = null, ILogger<TravessiaService>? logger = null,
    TimeProvider? timeProvider = null)
{
    public const string FonteUrl = "https://acquavias.com.br/santos-guaruja/";
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(3);
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly HttpClient client = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
    private readonly SemaphoreSlim gate = new(1, 1);
    private DadosTravessia? lastValid;
    private DateTimeOffset nextRefresh;

    public DadosTravessia Consultar() => lastValid is null ? Indisponivel(clock.GetUtcNow()) : MarkStale(lastValid);

    public async Task<DadosTravessia> ConsultarAsync(CancellationToken ct = default)
    {
        if (lastValid is not null && clock.GetUtcNow() < nextRefresh) return MarkStale(lastValid);
        await gate.WaitAsync(ct);
        try
        {
            if (lastValid is not null && clock.GetUtcNow() < nextRefresh) return MarkStale(lastValid);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(12));
            try
            {
                using var response = await client.GetAsync(FonteUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentType?.MediaType != "text/html")
                    throw new InvalidDataException("A fonte não retornou HTML.");
                var html = await response.Content.ReadAsStringAsync(timeout.Token);
                var now = clock.GetUtcNow();
                var parsed = Parse(html, now);
                lastValid = parsed;
                nextRefresh = now.Add(RefreshInterval);
                return parsed;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException or InvalidDataException)
            {
                ct.ThrowIfCancellationRequested();
                logger?.LogWarning("Consulta AcquaVias falhou ({ErrorType}): {Message}", ex.GetType().Name, ex.Message);
                nextRefresh = clock.GetUtcNow().Add(RefreshInterval);
                return lastValid is null ? Indisponivel(clock.GetUtcNow()) : MarkStale(lastValid);
            }
        }
        finally { gate.Release(); }
    }

    public static DadosTravessia Parse(string html, DateTimeOffset capturedAt)
    {
        // O identificador 1951 e os seletores são observados na página oficial.
        var topbar = Regex.Match(html, "data-travessia-id=[\\\"']1951[\\\"'](?<body>.*?)(?:</a>)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!topbar.Success) throw new InvalidDataException("Bloco da travessia Santos–Guarujá ausente.");
        var body = topbar.Groups["body"].Value;
        if (!body.Contains("Santos", StringComparison.OrdinalIgnoreCase) ||
            !body.Contains("Guaruj", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("O bloco identificado não corresponde à travessia Santos–Guarujá.");
        var timesBlock = Regex.Match(body, "aqv-topbar__tempos[^>]*>(?<body>.*?)</div>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var times = timesBlock.Success
            ? Regex.Matches(timesBlock.Groups["body"].Value, "<span[^>]*>\\s*(?<value>\\d{1,4})\\s*min\\s*</span>", RegexOptions.IgnoreCase | RegexOptions.Singleline)
            : Regex.Matches(string.Empty, "(?!)");
        if (times.Count != 2) throw new InvalidDataException("Tempos dos dois sentidos não encontrados.");
        var minutes = times.Cast<Match>().Select(m => int.Parse(m.Groups["value"].Value, CultureInfo.InvariantCulture)).ToArray();
        if (minutes.Any(value => value > 1440)) throw new InvalidDataException("Espera fora do limite esperado.");
        var vessel = Regex.Match(body, "aqv-topbar__embarcacoes[^>]*aria-label=[\\\"'](?<count>\\d+) embarcações? em operação[\\\"']", RegexOptions.IgnoreCase);
        var vesselCount = vessel.Success ? int.Parse(vessel.Groups["count"].Value, CultureInfo.InvariantCulture) : (int?)null;
        var notices = Regex.Matches(html, "<div[^>]*class=[\\\"'][^\\\"']*aqv-aviso-usuario[^\\\"']*[\\\"'][^>]*>(?<text>.*?)</div>", RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Select(m => WebUtility.HtmlDecode(Regex.Replace(m.Groups["text"].Value, "<[^>]+>", " ")).Trim())
            .Where(text => text.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var stale = false;
        var directions = new[] {
            new SentidoTravessia("Santos → Guarujá", minutes[0], null, capturedAt, capturedAt, true, stale),
            new SentidoTravessia("Guarujá → Santos", minutes[1], null, capturedAt, capturedAt, true, stale)
        };
        return new("acquavias_html", "AcquaVias · fonte oficial", FonteUrl,
            notices.Length == 0 ? "Tempos publicados pela AcquaVias. O site não informa o horário original da medição; exibimos quando a página foi consultada." : string.Join(" ", notices),
            60, vesselCount, notices, directions,
            new(FonteUrl, "AcquaVias · fonte oficial", true, capturedAt, "Página oficial consultada com sucesso."), capturedAt);
    }

    private DadosTravessia MarkStale(DadosTravessia data)
    {
        var stale = clock.GetUtcNow() - data.CapturadoEm.GetValueOrDefault() > StaleAfter;
        return data with {
            Aviso = stale ? "Dados da última consulta bem-sucedida; a fonte não foi atualizada nos últimos 3 minutos." + (data.Comunicados.Count == 0 ? "" : " " + string.Join(" ", data.Comunicados)) : data.Aviso,
            Sentidos = data.Sentidos.Select(s => s with { Desatualizado = stale }).ToArray(),
            ConsultaExterna = data.ConsultaExterna is null ? null : data.ConsultaExterna with { VerificadoEm = data.CapturadoEm }
        };
    }

    private DadosTravessia Indisponivel(DateTimeOffset now) => new("indisponivel", "AcquaVias · fonte oficial", FonteUrl,
        "Não foi possível consultar os dados oficiais da travessia. Tente novamente mais tarde.", 60, null,
        Array.Empty<string>(), new[] {
            new SentidoTravessia("Santos → Guarujá", null, null, null, null, false, false),
            new SentidoTravessia("Guarujá → Santos", null, null, null, null, false, false)
        }, new(FonteUrl, "AcquaVias · fonte oficial", false, now, "A fonte oficial está indisponível."));

    public object Historico(string periodo)
    {
        if (periodo is not ("horas" or "hoje" or "7dias")) throw new ArgumentException("Período inválido. Use horas, hoje ou 7dias.");
        return new { periodo, itens = Array.Empty<object>(), suficiente = false, aviso = "O histórico ainda não está disponível." };
    }
}

public sealed class TravessiaWorker(TravessiaService service, ILogger<TravessiaWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await service.ConsultarAsync(stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await service.ConsultarAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning("Atualização periódica da travessia falhou: {Message}", ex.Message); }
        }
    }
}
