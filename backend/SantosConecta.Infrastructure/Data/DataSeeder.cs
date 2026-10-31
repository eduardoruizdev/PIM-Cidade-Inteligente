using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SantosConecta.Domain.Entities;
namespace SantosConecta.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task PopularAsync(SantosConectaDbContext db, string senha)
    {
        if (senha.Length < 12) throw new ArgumentException("Defina Seed__Password com pelo menos 12 caracteres.");
        if (await db.Ocorrencias.AnyAsync() || await db.Funcionarios.AnyAsync() || await db.Cidadaos.AnyAsync())
            throw new InvalidOperationException("O seed só pode ser executado em tabelas sem cidadãos, funcionários e ocorrências. Nenhum dado existente foi substituído.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        var nomesCategorias = new[] { "Vias públicas", "Iluminação pública", "Limpeza urbana", "Alagamentos", "Calçadas", "Sinalização", "Arborização", "Vazamentos", "Acessibilidade", "Outros" };
        if (!await db.Categorias.AnyAsync())
            db.Categorias.AddRange(nomesCategorias.Select(n => new Categoria { Nome = n }));
        var nomesFuncionarios = new[] { "Ana Carolina Silva", "Ricardo Almeida", "Mariana Costa", "Pedro Santos", "Juliana Ferreira" };
        var hasher = new PasswordHasher<Funcionario>();
        var funcionarios = nomesFuncionarios.Select((nome, i) =>
        {
            var f = new Funcionario { Nome = nome, Email = i == 0 ? "ana.silva@exemplo.invalid" : $"funcionario{i}@exemplo.invalid", Perfil = i == 0 ? "Administrador" : "Funcionário" };
            f.SenhaHash = hasher.HashPassword(f, senha); return f;
        }).ToList();
        db.Funcionarios.AddRange(funcionarios);
        var cidadaos = Enumerable.Range(1, 24).Select(i => new Cidadao { Nome = $"Cidadão fictício {i:00}", Email = $"cidadao{i:00}@exemplo.invalid", Telefone = "00000000000" }).ToList();
        db.Cidadaos.AddRange(cidadaos);
        await db.SaveChangesAsync();
        var categorias = await db.Categorias.OrderBy(c => c.IdCategoria).ToListAsync();
        var bairros = new (string Nome, decimal Lat, decimal Lng)[] {
            ("Gonzaga",-23.9678m,-46.3336m),("Boqueirão",-23.969m,-46.3228m),("Embaré",-23.9723m,-46.3131m),
            ("Aparecida",-23.976m,-46.305m),("Ponta da Praia",-23.984m,-46.299m),("Campo Grande",-23.9574m,-46.342m),
            ("Vila Mathias",-23.9468m,-46.326m),("Marapé",-23.96m,-46.349m),("Macuco",-23.958m,-46.309m),
            ("José Menino",-23.967m,-46.3485m),("Centro",-23.936m,-46.325m) };
        var titulos = new[] { "Buraco na via prejudica a circulação", "Luminária apagada no período noturno", "Acúmulo de resíduos na calçada", "Ponto de alagamento após chuva", "Calçada com piso danificado", "Placa de sinalização danificada", "Árvore com galhos comprometidos", "Vazamento de água na via", "Rampa de acesso obstruída", "Manutenção de equipamento urbano" };
        var status = new[] { "Aberta", "Em análise", "Em andamento", "Resolvida", "Resolvida", "Cancelada", "Em andamento", "Resolvida" };
        var prioridades = new[] { "Baixa", "Média", "Média", "Alta", "Média", "Crítica", "Média" };
        var referencia = DateTime.UtcNow;
        for (var i = 0; i < 132; i++)
        {
            var bairro = bairros[new[] { 0, 0, 0, 1, 1, 2, 2, 3, 4, 5, 6, 7, 8, 9, 10 }[i % 15]];
            var categoria = i % categorias.Count;
            var abertura = referencia.AddDays(-((i * 17) % 89)).AddHours(-(i % 12));
            var o = new Ocorrencia
            {
                Protocolo = $"DEMO-{referencia.Year}-{i + 1:00000}",
                Titulo = titulos[categoria % titulos.Length],
                Descricao = "Registro inteiramente fictício para apresentação acadêmica. A situação informada requer avaliação do setor responsável. Não corresponde a uma reclamação real da Prefeitura de Santos.",
                Bairro = bairro.Nome,
                Endereco = $"Ponto fictício {i + 1}, {bairro.Nome}, Santos/SP",
                Latitude = bairro.Lat + (i % 5 - 2) * 0.0004m,
                Longitude = bairro.Lng + (i % 7 - 3) * 0.0003m,
                DataAbertura = abertura,
                Status = status[(i * 3 + i / 11) % status.Length],
                Prioridade = prioridades[(i * 5) % prioridades.Length],
                IdCategoria = categorias[categoria].IdCategoria,
                IdCidadao = cidadaos[i % cidadaos.Count].IdCidadao,
                IdFuncionario = funcionarios[i % funcionarios.Count].IdFuncionario
            };
            o.Historico.Add(new HistoricoOcorrencia
            {
                IdFuncionario = funcionarios[0].IdFuncionario,
                Descricao = "Reclamação fictícia cadastrada para apresentação acadêmica.",
                DataAlteracao = abertura
            });
            if (o.Status != "Aberta") o.Historico.Add(new HistoricoOcorrencia
            {
                IdFuncionario = funcionarios[0].IdFuncionario,
                Descricao = $"Status alterado para {o.Status}. Movimentação demonstrativa.",
                DataAlteracao = abertura.AddDays(Math.Min((referencia - abertura).TotalDays, 1 + i % 12))
            });
            db.Ocorrencias.Add(o);
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
