# Registro de validação

Data: 31/10/2026. O quadro inicial registra a entrega anterior. A investigação específica do SQL Server está na seção de revalidação ao final, com resultados desta rodada. Não presumir que configurar o banco em outra máquina equivale a executar os testes.

| Verificação | Resultado observado |
|---|---|
| `npm.cmd run typecheck` | Aprovado. |
| `npm.cmd run build` | Aprovado; artefatos em dist. |
| `dotnet build SantosConecta.sln --no-restore` | Aprovado, quatro projetos net8.0, zero erros e avisos. |
| Iniciar API | Aprovado, http://localhost:5080. |
| Swagger e contrato OpenAPI | HTTP 200; endpoints requeridos presentes. |
| Seis recursos de negócio sem sessão | HTTP 401, sem redirecionamento HTML. |
| Login com campos inválidos | HTTP 400 por Data Annotations. |
| Mutação a partir de origem externa | HTTP 403. |
| Vite local | HTTP 200 em http://127.0.0.1:5173. |
| Login em desktop/celular | Teste aprovado; sem transbordamento horizontal. |
| Sessão antiga em localStorage | Não concede acesso ao painel; teste aprovado. |
| Tentativa de login inválida | Apresenta erro e permanece no login; teste aprovado. |
| Conexão SQL Server | Não estabelecida: erros SSPI/credenciais Windows e TLS nativo; /health=503. |
| Aplicação dos scripts SQL e seed | Não executada: depende da conexão válida. |
| Cadastro/edição/status/histórico persistidos | Teste preparado, não executado por indisponibilidade do SQL. |
| Atualização do Dashboard/mapa com SQL | Teste preparado, não executado por indisponibilidade do SQL. |
| E-mail simulado persistido no SQL | Teste preparado, não executado por indisponibilidade do SQL. |
| SMTP real e Groq real | Não testados; sem credenciais externas configuradas. |

O teste HTTP básico executou nove verificações aprovadas. O teste de navegador resultou em **3 aprovados e 3 ignorados**, com motivo explícito. O sistema não usa mocks para fazer a integração aparentar sucesso.

## Impedimento encontrado

Foram identificadas instâncias SQL Server locais em execução, `sqlcmd` e LocalDB instalados. As tentativas de autenticação integrada ao servidor padrão, à instância nomeada e ao LocalDB falharam com erros de SSPI/credenciais do pacote de segurança. O SqlClient da API também informou indisponibilidade do suporte nativo de criptografia durante a conexão. Nenhum banco existente foi removido e nenhuma senha foi alterada.

Para concluir a validação persistida, configure a instância e uma conexão autorizada em `ConnectionStrings__SantosConecta` ou `appsettings.Local.json`, execute os dois scripts de banco, rode o seed e repita `npm.cmd run test:api` e `npm.cmd test` com `E2E_PASSWORD`. Não envie senhas por mensagem.

## Evidências geradas

- `artifacts/api-verification.json`: resultado das verificações HTTP desta execução.
- `artifacts/login-desktop.png` e `artifacts/login-mobile.png`: capturas da tela atual.
- `test-results/`: resultado temporário do Playwright.

Os artefatos são ignorados pelo Git. Capturas antigas de uma versão demonstrativa não comprovam a integração atual.

## Revalidação específica da conexão SQL Server — 31/10/2026

### Destino e configuração efetiva

Não havia `appsettings.Local.json` nem variável de conexão sobrescrevendo o destino nesta execução. Antes da correção, a configuração vinha de `appsettings.json` e apontava para `Server=localhost`, instância padrão `MSSQLSERVER`, autenticação Windows, `Encrypt=True` e `TrustServerCertificate=True` em todos os ambientes.

| Item verificado | Resultado |
|---|---|
| Instância pretendida | Padrão: `MSSQLSERVER`, instalação `MSSQL16.MSSQLSERVER`. |
| Serviço `MSSQLSERVER` | Running, início automático. Não foi reiniciado ou reconfigurado. |
| TCP da instância padrão | Habilitado, porta fixa 1433; conexão TCP a `127.0.0.1:1433` aprovada. |
| Modo de autenticação do servidor padrão | Misto (`LoginMode=2`), conforme registro local. |
| Autenticação usada pela aplicação | Windows (`Integrated Security=True`); não há login SQL configurado. |
| Contexto do executor | Conta Windows isolada; não é a identidade interativa usada para abrir o SSMS. Nome de usuário omitido. |
| ForceEncryption da instância padrão | 0, conforme registro. A mensagem genérica do driver não comprova que o servidor esteja forçando TLS. |
| Outra instância instalada | `MSSQLSERVER01`, serviço Running, autenticação Windows, TCP desabilitado. Não é o destino configurado. |
| SQL Browser | Stopped/Disabled; o destino TCP explícito na porta 1433 não depende dele. |
| Nova configuração de Development | `tcp:localhost,1433`, `SantosConectaDB`, Windows, `Encrypt=True`, `TrustServerCertificate=True`. |
| Origem da nova configuração | `appsettings.Development.json`, confirmada pelo diagnóstico da API. |
| Configuração base/Production | `Encrypt=True`, `TrustServerCertificate=False`, confirmada com `--no-launch-profile`. |
| Tentativa de TrustServerCertificate=True em Production | Inicialização recusada pela validação, antes de abrir conexão. |
| Compilação após a alteração | Quatro projetos compilados; zero erros e zero avisos. |
| API após reinicialização | Em execução em `http://localhost:5080`; nove verificações HTTP básicas novamente aprovadas. |
| Saúde do SQL após reinicialização | `/health` continua retornando 503. |

### Erro da conexão real usando o mesmo SqlClient da API

Comando executado:

```powershell
dotnet run --project backend/SantosConecta.API --no-build --no-restore -- --diagnosticar-sql
```

Etapa exata: **1 — `SqlConnection.OpenAsync()` para `master`, com a autenticação Windows e as opções TLS efetivas da API**. A conexão não chegou à consulta `SELECT`, à abertura de `SantosConectaDB` pelo EF Core ou à verificação de tabelas. Saída 2.

Mensagem completa do erro SQL, sem truncar o texto:

```text
Microsoft.Data.SqlClient.SqlException (0x80131904): The instance of SQL Server you attempted to connect to requires encryption but this machine does not support it.
Error Number:20,State:0,Class:20
```

O stack trace completo da execução está no arquivo local ignorado pelo Git `artifacts/sql-diagnostico-atual.txt`. A mensagem indica falha da negociação criptográfica do cliente antes da autenticação; não é a mensagem de cadeia de certificados não confiável que TrustServerCertificate costuma contornar. Não foi possível concluir qual componente nativo/credencial do ambiente isolado causa essa indisponibilidade, pois a conexão não avança além dessa etapa.

Os testes de frontend e navegador da entrega anterior não foram repetidos nesta rodada: não houve alteração no frontend e a pré-condição de SQL autenticado para os testes persistidos continua ausente. Os novos testes foram a compilação C#, as tentativas reais de abertura SQL, as duas verificações da política de Production e a repetição das nove verificações HTTP.

### Testes independentes de diagnóstico

O ODBC/sqlcmd também falhou na abertura da conexão, com estas mensagens completas:

```text
Microsoft ODBC Driver 17 for SQL Server : A criptografia não é suportada pelo cliente.
Microsoft ODBC Driver 17 for SQL Server : Provedor SSL: Credenciais não disponíveis no pacote de segurança
Microsoft ODBC Driver 17 for SQL Server : O cliente não pôde estabelecer a conexão.
Microsoft ODBC Driver 17 for SQL Server : Erro relatado pela rede ou específico à instância ao estabelecer conexão com o SQL Server. O servidor não foi encontrado ou não está acessível. Verifique se o nome da instância está correto e se o SQL Server está configurado para permitir conexões remotas. Para obter mais informações, consulte os Manuais Online do SQL Server.
```

Para separar o erro de TLS da autenticação Windows, foi feita uma tentativa independente, somente leitura, com System.Data.SqlClient, `master`, `tcp:127.0.0.1,1433` e `Encrypt=False` **apenas naquele objeto de diagnóstico**. Não houve alteração da configuração da API, do SQL Server ou do Windows. Não foi executado SELECT porque a abertura falhou:

```text
System.Data.SqlClient.SqlException: O nome da entidade de destino está incorreto. Não é possível gerar contexto SSPI.
Number=0; State=0; Class=11
```

Esse teste não é uma solução e a aplicação continua exigindo criptografia. Ele mostra que remover Encrypt da conexão também não produziu uma autenticação válida no contexto atual. Não foram alterados SPNs, TLS do sistema, permissões ou senhas.

### Pré-condição não satisfeita e próximos passos

Não foi confirmada uma conexão SQL autenticada. Portanto, **nesta revalidação não foram executados os scripts, o seed, operações CRUD, alterações de status/histórico ou testes de dashboard/mapa contra o banco**. A existência e o conteúdo de `SantosConectaDB` continuam não verificados; nenhum dado existente foi apagado ou substituído.

Configuração não sensível para repetir o teste em um terminal Windows normal, sob uma identidade já autorizada a acessar a instância no SSMS:

```text
Ambiente: Development
Server=tcp:localhost,1433;Database=SantosConectaDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5
```

O perfil de `dotnet run --project backend/SantosConecta.API` já seleciona Development. Execute `npm.cmd run diagnosticar:sql` antes de qualquer preparação do banco. Testar fora do executor, com a identidade autorizada, ainda precisa ser feito; não há garantia registrada de sucesso nessa condição.

Se a aplicação precisar usar autenticação SQL, configure **um login existente e autorizado** somente por `ConnectionStrings__SantosConecta` ou pelo arquivo local ignorado pelo Git, com `Integrated Security=False`. Não envie credenciais no chat e não as versione. A instância padrão já aceita modo misto; não é necessário mudar políticas globais ou a instância para isso. A disponibilidade de TLS no contexto que executa a API ainda deverá ser verificada.

Somente depois da etapa 1 confirmada, confira a existência do banco e suas tabelas. Aplique os scripts apenas se necessários e execute o seed apenas se a base estiver vazia. Em seguida, execute os testes de API e navegador com as credenciais de teste locais. Se TLS/SSPI continuar falhando no terminal normal, o responsável pelo ambiente Windows/SQL deve conferir Schannel e a autenticação da identidade do processo; não há evidência que justifique alterar SPNs ou desabilitar TLS globalmente.

Referências: [opções de conexão Microsoft.Data.SqlClient](https://learn.microsoft.com/en-us/sql/connect/ado-net/connection-options?view=sql-server-ver17) e [validação de certificados](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlconnectionstringbuilder.trustservercertificate?view=sqlclient-dotnet-standard-5.2).

### Atualização após confirmação do nome PCEDUARDO

O usuário informou `PCEDUARDO` como nome do servidor. A configuração Development e o exemplo local foram ajustados para `Server=tcp:PCEDUARDO,1433`, mantendo autenticação Windows, Encrypt=True e TrustServerCertificate=True exclusivamente em desenvolvimento.

O serviço MSSQLSERVER foi novamente confirmado como Running, e uma conexão TCP real a `PCEDUARDO:1433` foi estabelecida. O comando de diagnóstico confirmou o novo destino efetivo, mas continuou falhando na etapa 1, ao abrir master, com a mesma SqlException de TLS número 20 registrada acima. Código de saída 2. O relatório local é `artifacts/sql-diagnostico-pceduardo.txt`.

A mudança do nome do servidor não confirmou autenticação SQL. Scripts, seed e gravações não foram executados. O próximo teste permanece sendo o diagnóstico em um terminal Windows normal, sob uma conta já autorizada a conectar pelo SSMS.

### Diagnóstico executado pelo usuário no terminal Windows

O usuário enviou o resultado de `npm.cmd run diagnosticar:sql`, executado no seu terminal normal. A etapa 1 passou: conexão real com master confirmada, instância MSSQLSERVER, destino tcp:PCEDUARDO,1433, autenticação Windows, Encrypt=True e TrustServerCertificate=True. Assim, TLS e autenticação com o servidor funcionaram nesse contexto.

A consulta informou `Banco de destino visível: False`. A etapa 2 falhou ao abrir SantosConectaDB pelo DbContext:

```text
Não é possível abrir o banco de dados "SantosConectaDB" solicitado pelo logon. Falha de logon.
Falha de logon do usuário '[identidade do processo]'.
Error Number:4060,State:1,Class:11
```

Essa evidência substitui a hipótese de falha TLS para o terminal do usuário. A pendência agora é a existência/disponibilidade do banco de destino ou a permissão da identidade Windows sobre ele. Banco não visível não comprova, por si só, que ele não exista.

Os scripts existentes foram conferidos: 01-banco.sql condiciona CREATE DATABASE a DB_ID ausente e não contém exclusão de dados; 02-procedures.sql cria/altera as três procedures. A próxima ação é preparar a estrutura com uma identidade autorizada, sem recriar um banco existente, e repetir o diagnóstico. Se o banco já existir e o erro 4060 persistir, corrigir o acesso com o administrador da instância, sem alterar TLS. Seed, CRUD e verificações de dashboard/mapa continuam não confirmados nesta etapa.

### Conexão e estrutura confirmadas no terminal do usuário

Em novo resultado enviado pelo usuário, `npm.cmd run diagnosticar:sql` concluiu as três etapas sem erro. A conexão com master foi autenticada na instância MSSQLSERVER, SantosConectaDB passou a estar visível e a abertura do banco pelo DbContext foi confirmada. As consultas EF Core às seis tabelas retornaram:

| Tabela | Registros |
|---|---:|
| Cidadao | 0 |
| Funcionario | 0 |
| Categoria | 0 |
| Ocorrencia | 0 |
| HistoricoOcorrencia | 0 |
| EmailEnviado | 0 |

Portanto, servidor, autenticação Windows, TLS, acesso ao banco e leitura das seis tabelas estão confirmados **no terminal Windows do usuário**, com Encrypt=True e TrustServerCertificate=True em Development. A base está vazia; não é necessário recriá-la. O diagnóstico é somente leitura e não realizou a carga inicial.

Próxima etapa: executar `npm.cmd run api:seed` no mesmo terminal, definindo antes Seed__Password com pelo menos 12 caracteres. Seed, login da plataforma, CRUD persistido, histórico e indicadores/mapa ainda aguardam execução e verificação após a carga. A API iniciada pelo executor foi encerrada para liberar a porta; o backend deve ser iniciado pelo usuário com `npm.cmd run api`, usando a identidade Windows em que a conexão foi validada.

### Carga inicial e diagnóstico do login na porta 5174

O usuário enviou a mensagem de conclusão do seed: 132 ocorrências fictícias e nenhum e-mail enviado. A senha não é registrada nesta documentação. Em seguida, o usuário informou erro genérico de login ao acessar `http://127.0.0.1:5174/login`.

A API iniciada pelo usuário respondeu `/health` com 200. O proxy do frontend na porta 5173 respondeu `/api/auth/me` com 401, resultado esperado sem sessão. Testes de POST com senha propositalmente curta confirmaram a diferença: a origem 5173 chegou à validação de modelo (400), mas a origem 5174 recebeu 403 antes de verificar credenciais. A configuração autorizava somente a porta 5173.

A configuração Development foi ajustada para permitir localhost e 127.0.0.1 nas portas 5173 e 5174, sem wildcard e sem ampliar a configuração de Production. O bloqueio de origem agora devolve ProblemDetails com uma explicação, em vez de uma resposta vazia que virava erro genérico no frontend.

A Solution compilou com zero erros e avisos em diretório de saída separado, preservando o backend em uso pelo usuário. Uma instância temporária na porta 5081 confirmou que as quatro origens locais chegam à validação (400 com corpo inválido), e uma origem externa continua bloqueada com 403 e mensagem explicativa. Esses testes não utilizaram a senha real e não realizaram gravações no banco. A instância temporária é encerrada após a verificação.

O backend do usuário na porta 5080 precisa ser reiniciado no seu próprio terminal para carregar a nova lista AllowedOrigins e o código compilado. O login com a senha cadastrada e os testes CRUD autenticados continuam aguardando confirmação após esse reinício.
