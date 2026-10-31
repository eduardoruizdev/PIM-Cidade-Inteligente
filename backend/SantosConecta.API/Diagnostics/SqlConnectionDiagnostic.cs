using System.Security.Principal;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SantosConecta.Infrastructure.Data;

namespace SantosConecta.API.Diagnostics;

// Diagnóstico local via CLI. Não cria tabelas, não popula dados e não expõe credenciais.
public static class SqlConnectionDiagnostic
{
    public static async Task<int> ExecutarAsync(IServiceProvider services, IConfiguration configuration, string ambiente)
    {
        var options = new SqlConnectionStringBuilder(configuration.GetConnectionString("SantosConecta"));
        var source = ((IConfigurationRoot)configuration).Providers.Reverse()
            .FirstOrDefault(p => p.TryGet("ConnectionStrings:SantosConecta", out _))?.ToString();
        Console.WriteLine($"Ambiente: {ambiente}");
        Console.WriteLine($"Origem da configuração: {source}");
        Console.WriteLine($"Servidor: {options.DataSource}");
        Console.WriteLine($"Banco: {options.InitialCatalog}");
        Console.WriteLine($"Autenticação: {(options.IntegratedSecurity ? "Windows (identidade do processo)" : "SQL Server / autenticação configurada")}");
        Console.WriteLine($"Encrypt: {options.Encrypt}; TrustServerCertificate: {options.TrustServerCertificate}");

        var etapa = "1 — abrir conexão ao servidor (master), com a mesma autenticação e TLS da API";
        try
        {
            // O destino master permite separar banco inexistente de falha de TLS/autenticação.
            var masterOptions = new SqlConnectionStringBuilder(options.ConnectionString) { InitialCatalog = "master", Pooling = false };
            Console.WriteLine($"Etapa {etapa}");
            await using (var connection = new SqlConnection(masterOptions.ConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT CONVERT(nvarchar(128), SERVERPROPERTY('InstanceName')), DB_ID(@banco)";
                command.Parameters.Add("@banco", System.Data.SqlDbType.NVarChar, 128).Value = options.InitialCatalog;
                await using var reader = await command.ExecuteReaderAsync();
                await reader.ReadAsync();
                Console.WriteLine($"Conexão real confirmada. Instância: {(reader.IsDBNull(0) ? "MSSQLSERVER (padrão)" : reader.GetString(0))}");
                Console.WriteLine($"Banco de destino visível: {!reader.IsDBNull(1)}");
            }

            etapa = "2 — abrir o banco de destino pelo DbContext do EF Core";
            Console.WriteLine($"Etapa {etapa}");
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SantosConectaDbContext>();
            await db.Database.OpenConnectionAsync();
            Console.WriteLine("Conexão ao banco confirmada.");

            etapa = "3 — consultar tabelas existentes pelo EF Core, sem gravações";
            Console.WriteLine($"Etapa {etapa}");
            Console.WriteLine($"Cidadãos: {await db.Cidadaos.CountAsync()}");
            Console.WriteLine($"Funcionários: {await db.Funcionarios.CountAsync()}");
            Console.WriteLine($"Categorias: {await db.Categorias.CountAsync()}");
            Console.WriteLine($"Ocorrências: {await db.Ocorrencias.CountAsync()}");
            Console.WriteLine($"Históricos: {await db.Historicos.CountAsync()}");
            Console.WriteLine($"E-mails: {await db.Emails.CountAsync()}");
            Console.WriteLine("Diagnóstico concluído. Nenhum dado foi alterado.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FALHA NA ETAPA {etapa}");
            Console.Error.WriteLine(OcultarSegredos(ex.ToString(), options));
            if (ex is SqlException sql)
                foreach (SqlError error in sql.Errors)
                    Console.Error.WriteLine($"SQL Number={error.Number}; State={error.State}; Class={error.Class}; Message={OcultarSegredos(error.Message, options)}");
            Console.Error.WriteLine("Scripts SQL e seed não executados. O diagnóstico é somente leitura.");
            return 2;
        }
    }

    private static string OcultarSegredos(string text, SqlConnectionStringBuilder options)
    {
        foreach (var value in new[] { options.Password, options.UserID })
            if (!string.IsNullOrWhiteSpace(value)) text = text.Replace(value, "[oculto]", StringComparison.OrdinalIgnoreCase);
        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
            if (!string.IsNullOrWhiteSpace(identity.Name))
                text = text.Replace(identity.Name, "[identidade do processo]", StringComparison.OrdinalIgnoreCase);
        }
        return text;
    }
}
