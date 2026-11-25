using SantosConecta.Infrastructure.Services;
using SantosConecta.API;
using Microsoft.Extensions.Configuration;

var validJpeg = new UploadFoto("phone.jpg", "image/jpeg", [0xff, 0xd8, 0xff, 0xe0, 0x00, 0x02, 0xff, 0xd9]);
FotosOcorrenciaService.Validar([validJpeg]);
Check(true, "JPEG válido aceito");
FotosOcorrenciaService.Validar([new("photo.png", "image/png", ValidPng())]);
Check(true, "PNG válido aceito");
Reject([new("image.png", "image/png", [137, 80, 78, 71, 13, 10, 26, 10])], "imagem truncada rejeitada");
Reject([new("program.exe", "application/octet-stream", [0x4d, 0x5a, 0x90, 0x00])], "executável rejeitado");
Reject([new("payload.jpg", "image/jpeg", [0x4d, 0x5a, 0x90, 0x00])], "executável disfarçado de JPEG rejeitado");
Reject([new("image.png", "image/jpeg", [137, 80, 78, 71, 13, 10, 26, 10])], "MIME incompatível rejeitado");
Reject([new("image.gif", "image/gif", [0x47, 0x49, 0x46])], "formato não permitido rejeitado");
Reject([new("large.jpg", "image/jpeg", new byte[FotosOcorrenciaService.MaxBytes + 1])], "limite de 8 MB aplicado");
Reject(Enumerable.Range(0, 6).Select(_ => validJpeg).ToList(), "limite de cinco imagens aplicado");
Console.WriteLine("9 verificações de validação de fotografias aprovadas. Nenhum banco ou arquivo foi alterado.");

var oldDirectory = Directory.GetCurrentDirectory();
var oldGroq = Environment.GetEnvironmentVariable("GROQ_API_KEY");
var oldTomTom = Environment.GetEnvironmentVariable("TOMTOM_API_KEY");
var envFolder = Path.Combine(Path.GetTempPath(), "santos-dotenv-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(envFolder);
    await File.WriteAllTextAsync(Path.Combine(envFolder, ".env"), "GROQ_API_KEY=dotenv-test-groq\nTOMTOM_API_KEY=dotenv-test-tomtom\n");
    Environment.SetEnvironmentVariable("GROQ_API_KEY", null);
    Environment.SetEnvironmentVariable("TOMTOM_API_KEY", null);
    Directory.SetCurrentDirectory(envFolder);
    RootDotEnv.Load();
    var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
    Check(configuration["GROQ_API_KEY"] == "dotenv-test-groq" && configuration["TOMTOM_API_KEY"] == "dotenv-test-tomtom", "Chaves Groq e TomTom são carregadas pelo backend sem exibição");
}
finally
{
    Directory.SetCurrentDirectory(oldDirectory);
    Environment.SetEnvironmentVariable("GROQ_API_KEY", oldGroq);
    Environment.SetEnvironmentVariable("TOMTOM_API_KEY", oldTomTom);
    Directory.Delete(envFolder, true);
}

static void Reject(IReadOnlyList<UploadFoto> items, string name)
{
    try { FotosOcorrenciaService.Validar(items); throw new Exception($"Falhou: {name}"); }
    catch (ArgumentException) { Check(true, name); }
}
static void Check(bool ok, string name)
{
    if (!ok) throw new Exception(name);
    Console.WriteLine($"OK: {name}");
}
static byte[] ValidPng()
{
    byte[] bytes = new byte[45];
    new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(bytes, 0);
    bytes[11] = 13;
    "IHDR"u8.CopyTo(bytes.AsSpan(12));
    bytes[19] = 1;
    bytes[23] = 1;
    "IEND"u8.CopyTo(bytes.AsSpan(37));
    return bytes;
}
