using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SantosConecta.Domain.Entities;
using SantosConecta.Infrastructure.Data;

namespace SantosConecta.Infrastructure.Services;

public record UploadFoto(string NomeOriginal, string TipoDeclarado, byte[] Conteudo);
public record FotoOcorrenciaDto(int IdFoto, string TipoConteudo, long TamanhoBytes, DateTime DataEnvio, string Url);

public class FotosOcorrenciaService(SantosConectaDbContext db, IConfiguration config)
{
    public const int MaxFotos = 5;
    public const int MaxBytes = 8 * 1024 * 1024;
    private string Root
    {
        get
        {
            var configured = config["Photos:StoragePath"];
            if (!string.IsNullOrWhiteSpace(configured)) return Path.GetFullPath(configured);
            for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "SantosConecta.sln")) || File.Exists(Path.Combine(directory.FullName, ".env")))
                    return Path.Combine(directory.FullName, "App_Data", "fotos-ocorrencias");
            return Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "fotos-ocorrencias");
        }
    }

    public static void Validar(IReadOnlyList<UploadFoto> fotos)
    {
        if (fotos.Count > MaxFotos) throw new ArgumentException($"Anexe no máximo {MaxFotos} fotografias por reclamação.");
        foreach (var foto in fotos)
        {
            if (foto.Conteudo.Length is 0 or > MaxBytes) throw new ArgumentException("Cada imagem deve ter até 8 MB.");
            var ext = Path.GetExtension(foto.NomeOriginal).ToLowerInvariant();
            var (mime, signature) = ext switch
            {
                ".jpg" or ".jpeg" => ("image/jpeg", (Func<byte[], bool>)(b => b.Length >= 4 && b[0] == 0xff && b[1] == 0xd8 && b[2] == 0xff && b[^2] == 0xff && b[^1] == 0xd9)),
                ".png" => ("image/png", (Func<byte[], bool>)(b => b.Length >= 45 && b.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && b.AsSpan(12, 4).SequenceEqual("IHDR"u8) && BitConverter.ToUInt32(b, 16) > 0 && BitConverter.ToUInt32(b, 20) > 0 && b.AsSpan(b.Length - 8, 4).SequenceEqual("IEND"u8))),
                ".webp" => ("image/webp", (Func<byte[], bool>)(b => b.Length >= 20 && b.AsSpan(0, 4).SequenceEqual("RIFF"u8) && b.AsSpan(8, 4).SequenceEqual("WEBP"u8) && BitConverter.ToUInt32(b, 4) == b.Length - 8)),
                _ => throw new ArgumentException("Formato inválido. Envie imagens JPG, PNG ou WebP.")
            };
            if (!string.Equals(foto.TipoDeclarado, mime, StringComparison.OrdinalIgnoreCase) || !signature(foto.Conteudo))
                throw new ArgumentException("O conteúdo da imagem não corresponde ao formato informado.");
        }
    }

    public async Task<List<FotoOcorrenciaDto>> SalvarAsync(int idOcorrencia, IReadOnlyList<UploadFoto> fotos, CancellationToken ct)
    {
        Validar(fotos);
        if (fotos.Count == 0) return [];
        var dir = Path.Combine(Root, idOcorrencia.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(dir);
        var gravadas = new List<string>();
        try
        {
            var entities = new List<FotoOcorrencia>();
            foreach (var foto in fotos)
            {
                var ext = Path.GetExtension(foto.NomeOriginal).ToLowerInvariant();
                var nome = $"{Guid.NewGuid():N}{ext}";
                await File.WriteAllBytesAsync(Path.Combine(dir, nome), foto.Conteudo, ct);
                gravadas.Add(Path.Combine(dir, nome));
                entities.Add(new FotoOcorrencia { IdOcorrencia = idOcorrencia, NomeArmazenado = nome, TipoConteudo = foto.TipoDeclarado, TamanhoBytes = foto.Conteudo.LongLength, DataEnvio = DateTime.UtcNow });
            }
            db.FotosOcorrencias.AddRange(entities);
            await db.SaveChangesAsync(ct);
            return entities.Select(e => Map(e)).ToList();
        }
        catch
        {
            foreach (var file in gravadas) try { File.Delete(file); } catch (IOException) { }
            throw;
        }
    }

    public async Task<List<FotoOcorrenciaDto>?> ListarAsync(int idOcorrencia, CancellationToken ct)
    {
        if (!await db.Ocorrencias.AsNoTracking().AnyAsync(o => o.IdOcorrencia == idOcorrencia, ct)) return null;
        return await db.FotosOcorrencias.AsNoTracking().Where(f => f.IdOcorrencia == idOcorrencia).OrderBy(f => f.DataEnvio).Select(f => new FotoOcorrenciaDto(f.IdFoto, f.TipoConteudo, f.TamanhoBytes, f.DataEnvio, $"/api/ocorrencias/{idOcorrencia}/fotos/{f.IdFoto}")).ToListAsync(ct);
    }

    public async Task<(byte[] Bytes, string Mime)?> ObterAsync(int idOcorrencia, int idFoto, CancellationToken ct)
    {
        var foto = await db.FotosOcorrencias.AsNoTracking().FirstOrDefaultAsync(f => f.IdOcorrencia == idOcorrencia && f.IdFoto == idFoto, ct);
        if (foto is null) return null;
        var path = Path.GetFullPath(Path.Combine(Root, idOcorrencia.ToString(System.Globalization.CultureInfo.InvariantCulture), foto.NomeArmazenado));
        if (!path.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return null;
        return (await File.ReadAllBytesAsync(path, ct), foto.TipoConteudo);
    }

    public Task<int?> ProprietarioAsync(int idOcorrencia, CancellationToken ct) => db.Ocorrencias.AsNoTracking().Where(o => o.IdOcorrencia == idOcorrencia).Select(o => (int?)o.IdCidadao).FirstOrDefaultAsync(ct);
    private static FotoOcorrenciaDto Map(FotoOcorrencia f) => new(f.IdFoto, f.TipoConteudo, f.TamanhoBytes, f.DataEnvio, $"/api/ocorrencias/{f.IdOcorrencia}/fotos/{f.IdFoto}");
}
