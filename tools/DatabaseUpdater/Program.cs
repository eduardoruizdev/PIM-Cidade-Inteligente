using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

// Executar na raiz. Apenas scripts incrementais versionados; sem seed ou DROP.
var root = Directory.GetCurrentDirectory();
var api = Path.Combine(root,"backend","SantosConecta.API");
if (!Directory.Exists(api)) {Console.Error.WriteLine("Execute este comando na pasta raiz do projeto.");return 1;}
var config = new ConfigurationBuilder().SetBasePath(api)
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json",optional:true)
    .AddJsonFile("appsettings.Local.json",optional:true)
    .AddJsonFile("appsettings.Local.example.json",optional:true)
    .AddEnvironmentVariables().Build();
try
{
    await using var connection = new SqlConnection(config.GetConnectionString("SantosConecta"));
    await connection.OpenAsync();
    foreach (var name in new[]{"04-santos-conecta-v2.sql","05-localizacao.sql","06-notificacoes.sql","07-meteorologia.sql"})
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(root,"database",name));
        foreach (var batch in Regex.Split(sql,@"^\s*GO\s*;?\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
        {
            if(string.IsNullOrWhiteSpace(batch))continue;
            await using var command = new SqlCommand(batch,connection){CommandTimeout=60};
            await command.ExecuteNonQueryAsync();
        }
        Console.WriteLine("OK: "+name);
    }
    await using var verify = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo') AND name IN ('MensagemOcorrencia','LeituraMensagem','AlertaMeteorologico','PreferenciaMeteorologica','EntregaMeteorologica')",connection);
    if(Convert.ToInt32(await verify.ExecuteScalarAsync())!=5)throw new InvalidOperationException("Verificação de tabelas incompleta.");
    Console.WriteLine("Banco atualizado e cinco tabelas verificadas. Dados existentes preservados.");
    return 0;
}
catch(SqlException ex){Console.Error.WriteLine($"Falha SQL {ex.Number}. Nenhuma configuração de autenticação/TLS foi alterada. Execute no seu PowerShell com acesso ao banco.");return 1;}
