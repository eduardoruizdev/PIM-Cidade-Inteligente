# Evolução Santos Conecta — outubro de 2026

Projeto acadêmico fictício, sem vínculo com a Prefeitura de Santos. O React e a API existentes foram ampliados, com um único backend e SQL Server.

## Entrega

- Painel com Montserrat local e licença OFL, cores #25431D e #70BD54, marca própria em vetor, navegação, tabelas, formulários, login e estados responsivos. Referência: [Manual de Identidade de Santos](https://www.santos.sp.gov.br/?q=node/136896). Sem uso do brasão oficial.
- Aplicativo Android em `mobile/`: boas-vindas, cadastro sem CPF, autenticação, início, reclamação por etapas, categorias reais da API, localização por mapa ou GPS com permissão, protocolo, minhas reclamações, filtros, detalhes, histórico, respostas e perfil.
- Controllers → Services → Repositories → EF Core → SQL Server, sem microserviços, CQRS ou outro banco.
- Cookie ASP.NET protegido por Data Protection; Flutter armazena somente o ticket no armazenamento seguro do Android. Senhas usam `PasswordHasher<Cidadao>` e não são persistidas no dispositivo.
- Endpoints administrativos exigem Funcionário/Administrador. A identidade do cidadão vem dos claims; IdCidadao, status, responsável e prioridade externos não são aceitos como autoridade na abertura pública.
- Histórico público mostra registro e alterações de status, sem observações internas. Respostas aparecem somente com SMTP registrado como Enviado; uma simulação nunca aparece como entregue.
- Priorização Territorial Justa, revisão humana e histórico, fila administrativa e equidade no Dashboard. A página Relatórios continua removida.
- Groq exclusivamente no C#: resposta administrativa e sugestão opcional de categoria/gravidade. Não altera pontuação. Falhas preservam a classificação e redação manuais.
- Anexos não foram adicionados; foram priorizados os fluxos principais.

## Executar e banco

Use a conexão autorizada existente em `ConnectionStrings__SantosConecta`. Preserve TLS e segredos no servidor. Em Development, `appsettings.Local.json` é opcional e ignorado pelo Git.

1. Execute `npm.cmd run diagnosticar:sql`. Neste executor a conexão SQL funcionou fora do sandbox, sem alterar autenticação, privilégios ou TLS da instância.
2. Aplique `database/03-mobile-priorizacao.sql` depois dos dois scripts originais. O novo script é incremental e idempotente: preserva registros e tabelas, adiciona SenhaHash opcional, critérios da triagem e IndicadorTerritorial; permite autor de histórico nulo para o registro inicial do cidadão.
3. Execute `dotnet build SantosConecta.sln --no-restore`, `npm.cmd run api` e `npm.cmd run dev`.
4. App: [FLUTTER.md](FLUTTER.md). Cálculo: [PRIORIZACAO.md](PRIORIZACAO.md).

Não repita o seed inicial sobre a base preenchida. Cidadãos antigos não recebem senhas fictícias. E-mails já existentes requerem contato com a equipe, sem apropriar automaticamente o registro antigo.

## Rotas novas

| Rota | Acesso |
|---|---|
| POST `/api/auth/cadastro` | Público, limitado por IP |
| POST `/api/auth/login?publico=cidadao` | Login cidadão, limitado por IP |
| POST `/api/auth/login` | Login administrativo preservado |
| GET/PUT `/api/cidadaos/me` | Cidadão autenticado |
| GET `/api/ocorrencias/minhas` | Somente ocorrências do autor autenticado |
| GET `/api/ocorrencias/minhas/{id}` | Detalhe e respostas do autor autenticado |
| POST `/api/ocorrencias` | DTO conforme o perfil da sessão, sem confiar em IdCidadao do cidadão |
| GET `/api/priorizacao/fila`, `/indicadores`, `/equidade` | Funcionário/Administrador |
| PUT `/api/priorizacao/{id}` | Funcionário/Administrador, justificativa obrigatória |
| POST `/api/assistente/classificar` | Autenticado, sem efeito automático |
| POST `/api/assistente/gerar-resposta` | Funcionário/Administrador |

Aliases de compatibilidade: `/api/auth/cidadao/login` e POST `/api/ocorrencias/minhas`.

## Integrações

Groq: configure `GROQ_API_KEY` e `Groq__Simular=false` no processo do backend. O modelo é configurável com `Groq__Model`. Nunca use variáveis VITE ou arquivos Flutter para chaves. A classificação valida ID existente e gravidade de 0 a 40, sem indicadores territoriais. O cidadão confirma a categoria; a equipe decide gravidade e prioridade. Descrições são tratadas como dados não confiáveis; toda sugestão exige revisão.

SMTP continua com `Email__Simular=true` por padrão. Para envio real configure `SMTP_HOST`, `SMTP_PORT`, `SMTP_ENABLE_SSL`, `SMTP_FROM`, `SMTP_USER`, `SMTP_PASSWORD` e `Email__Simular=false`. Reinicie a API após mudar variáveis. Aceitação SMTP não comprova entrega na caixa postal. Não foram enviados e-mails externos na validação.

## Testes reproduzíveis

```powershell
npm.cmd run build
dotnet build SantosConecta.sln --no-restore
dotnet run --project tests/priority/PriorityChecks.csproj
# API de teste em Development com conexão SQL válida:
dotnet run --project backend/SantosConecta.API --no-build --no-restore -- --urls http://localhost:5081
# Em outro terminal:
$env:API_TEST_URL = 'http://localhost:5081'
node scripts/test-citizen.mjs
```

O teste integrado cria duas contas de cidadão, uma conta de **Funcionário sem privilégios de Administrador** e uma reclamação fictícias. Senhas aleatórias passam por variáveis de ambiente e não são impressas. A fixture CLI só funciona em Development. A ocorrência termina Cancelada e mantém histórico; não se apagam dados. O SQL real não é substituído por mocks.

Capturas: permita a origem `http://127.0.0.1:5175` na API, inicie Vite com `API_PROXY_TARGET=http://localhost:5081`, `VITE_API_URL` vazio e `--port 5175`, então execute `node scripts/test-citizen.mjs --browser`. Chrome grava `artifacts/premium-*.png`.

Resultados: [VALIDACAO-EVOLUCAO.md](VALIDACAO-EVOLUCAO.md). Compilar e testar widgets não comprova execução em Android físico.
