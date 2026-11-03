# Validação — atualização de 02/11/2026

## Modais, notificações e trânsito real

Resultados executados nesta atualização, com SQL Server real. As seções datadas de 01/11 abaixo são históricas.

| Verificação | Resultado observado |
|---|---|
| Build React | TypeScript e Vite aprovados após as alterações. |
| Build ASP.NET Core | Solution aprovada, zero erros e avisos. |
| Modal de funcionários | 10 verificações aprovadas: cadastro, edição sem trocar senha, foco, ESC, bloqueio durante salvamento, confirmação de situação, atualização da lista, login e autorização. Viewports de 390×844 e 1440×1000. |
| API, React, serviços Flutter e SQL | 75 verificações aprovadas na regressão final com trânsito habilitado; inclui mensagens, avisos, isolamento entre cidadãos, leitura idempotente e individual, atribuição de responsável e restrições da gestão. |
| Flutter analyze | Aprovado, sem problemas. |
| Flutter test com SQL real | Cinco testes aprovados com RUN_REAL_SQL=true. Somente armazenamento seguro do dispositivo é substituído no runner; HTTP e persistência SQL são reais. |
| Flutter web | Build JavaScript aprovado. Quatro testes padrão aprovados; o teste SQL é explicitamente ignorado nesse comando, sendo executado separadamente na regressão acima. |
| TomTom real no React | Validado em Orla e Av. Ana Costa, com fluxo e incidentes disponíveis. Uma consulta retornou um incidente na Av. Ana Costa; a Orla retornou lista vazia. Isso representa o instante consultado, não uma condição permanente. |
| Fluxo visual Flutter ↔ React | 15 verificações aprovadas na execução final com TomTom real: cadastro cidadão, ocorrência, resposta React, sino Flutter por polling, abertura e leitura, resposta Flutter, sino React e conversa destacada, persistência das duas mensagens, fluxo/incidentes TomTom e velocidade renderizada no Flutter. |
| Banco | Script incremental 06 aplicado com sucesso; leituras da equipe persistem em LeituraMensagem sem alterar leitura de outros usuários. |
| Proteção da chave | Arquivo appsettings.Local.example.json preservado no disco, retirado do índice Git e ignorado. Carregado somente em Development; chave somente no backend. |

Evidências: `artifacts/validacao-modais.json`, `artifacts/validacao-v2.json`, `artifacts/validacao-transito-real.json` e `artifacts/validacao-flutter-web-v2.json` (conferir a data de cada relatório). Capturas: `modal-390.png`, `modal-1440.png`, `transito-real-react.png` e `transito-real-mobile.png` em artifacts.

Ocorrências de execução corrigidas: processos antigos bloqueavam DLLs do backend; foram reiniciados somente os servidores deste projeto. Flutter precisou de execução fora do sandbox por bloqueio de telemetria. O endpoint TomTom Incident Details rejeitou `pt-BR` com HTTP 400; passou a responder após uso de `pt-PT`. Execuções intermediárias do teste visual falharam durante alternância de foco entre clientes e leitura do corpo HTTP na navegação Flutter; o runner passou a controlar o foco e capturar a resposta real antes de devolvê-la ao navegador. A central React também atualiza a lista ao abrir. Essas execuções intermediárias não são contabilizadas como aprovadas. Nenhuma chave foi impressa em logs ou relatórios.

Limites: fluxo por trecho próximo à referência, sem cobertura completa das ruas; incidentes por recorte retangular, podendo alcançar municípios vizinhos. Sem push com app fechado. APK, Android em aparelho físico, GPS físico e WASM não foram validados nesta atualização. Não foram contratados serviços ou alterados planos. O contador de requisições TomTom em memória não substitui o controle financeiro da conta do provedor. Geocodificação externa oscilou entre nenhum resultado e resultado válido; ambos os caminhos foram exercitados sem simular localização.

Configuração, autorização, regras de leitura e reprodução: [TRANSITO-E-NOTIFICACOES.md](TRANSITO-E-NOTIFICACOES.md).

---

# Registro anterior — Santos Conecta 2.0

Data: 01/11/2026. Este registro distingue testes executados de integrações preparadas. Os resultados históricos no final não substituem esta validação.

## Resultados observados nesta execução

| Verificação | Resultado e alcance |
|---|---|
| Build React | Aprovado: TypeScript e build Vite de produção. |
| Build ASP.NET Core | Aprovado: quatro projetos, zero erros e avisos na compilação final. |
| flutter analyze | Aprovado após correção de seis regras de blocos em ifs. |
| flutter test padrão | Quatro testes aprovados. O teste SQL é explicitamente ignorado quando RUN_REAL_SQL não é fornecido. |
| Flutter com HTTP/SQL reais | Cinco testes aprovados com RUN_REAL_SQL=true. Nenhuma resposta HTTP ou consulta SQL simulada; somente armazenamento seguro do dispositivo em memória no runner sem aparelho. |
| Regressão de cidadãos | 46 verificações aprovadas por scripts/test-citizen.mjs com SQL real. |
| Bateria 2.0 | 64 verificações aprovadas na execução final, incluindo filtros e período. Relatório em artifacts/validacao-v2.json, com navegador React e SQL real. |
| Gestão pela interface React | Cadastro, edição de cargo, desativação, reativação e login real da conta criada aprovados. artifacts/validacao-admin-web-v2.json. |
| Fluxo Flutter web ↔ React | 11 verificações aprovadas. Cadastro/login Flutter, bairro/rua, geocodificação, protocolo, resolução no React, consulta sem 404, mensagem React → Flutter, resposta Flutter → React e trânsito indisponível. artifacts/validacao-flutter-web-v2.json. |
| Build Flutter web | Build JavaScript de produção executado com sucesso; há avisos de dry-run WASM do armazenamento seguro. O build final foi repetido após os ajustes visuais. |
| Priorização | Seis verificações determinísticas aprovadas em tests/priority. |
| SQL Server real | Diagnóstico, scripts incrementais, gravações e consultas aprovados fora do sandbox. Nenhuma tabela foi recriada. |

O teste nativo dos serviços Flutter deixou a ocorrência interna **147**, Cancelada, LocalizacaoConfirmada=0 e **duas mensagens**. Esses valores foram confirmados diretamente por SELECT no SQL Server, além das respostas HTTP do teste. Não foram apagados registros para obter um resultado de teste limpo. Ao encerrar, 22 contas fictícias da evolução 2.0 foram desativadas logicamente; contas preexistentes do usuário não foram alteradas.

## Roteiro obrigatório e evidências

1. Builds React e ASP.NET Core: executados, aprovados.
2. Analyze e testes Flutter: executados; o teste SQL exige o modo explícito descrito acima.
3. Cadastro/login de funcionário: por API e pelo formulário React, com hash persistido.
4. Bloqueio administrativo: funcionário recebeu 403 em GET/POST/PUT de gestão. A rota operacional de responsáveis continua autorizada.
5. Ocorrência cadastrada pelo Flutter: executado tanto pelos serviços Dart como pela interface Flutter web.
6. Consulta pelo Flutter: lista e detalhes consultados com sessão do proprietário.
7. Status alterado pelo React: executado pelo controle real da tela de detalhes.
8. Status atualizado no Flutter sem 404: confirmado após resolução; o teste de serviços também percorreu Em análise, Em andamento, Resolvida e Cancelada.
9. Histórico: eventos de status permanecem consultáveis, inclusive após encerramento.
10. Equipe envia mensagem: formulário React e endpoint persistido.
11. Flutter recebe: exibição na interface e consulta pelos serviços Dart.
12. Cidadão responde: campo de resposta Flutter web e serviço Dart.
13. React recebe a resposta: texto exibido na conversa após atualização periódica.
14. Bairro e rua: seleção e pesquisa pela interface com bairro real do catálogo.
15. Localização automática: Nominatim retornou alternativas reais para Avenida Ana/Anna Costa, Gonzaga; o cidadão confirmou uma alternativa e a API persistiu as coordenadas. A confirmação por número agora exige correspondência do número retornado, evitando marcar um trecho de rua como número exato.
16. Trânsito React/Flutter: mapa de referência, região, legenda e estado de indisponibilidade verificados. **Dados de trânsito ao vivo não foram validados**.
17. Persistência: SQL Server real, incluindo SELECT independente do registro criado pelos serviços Flutter.
18. Desativação: sessão já aberta passou a receber 401; novo login recusado. Reativação permitiu login novamente.
19. Isolamento da conversa: outro cidadão recebeu 404 em leitura e envio. Conteúdo vazio e HTML foram recusados com 400.

## Falhas intermediárias e correções

- Dentro do executor restrito, SqlClient falhou com SQL 20: servidor exige criptografia e o executor não consegue fornecê-la. O diagnóstico autorizado fora do sandbox conectou à mesma instância e banco, sem desativar TLS ou mudar permissões do SQL.
- Flutter inicialmente não acessou seu arquivo de telemetria fora da pasta do projeto. O SDK foi executado com acesso autorizado ao cache existente.
- Foram corrigidos um fechamento de JSX, um fechamento da tela de notificações e seis informações de lint após formatação Dart.
- O servidor Vite de desenvolvimento excedeu o tempo de navegação em um teste. Os testes integrados passaram a usar o build estático com proxy local. Não foram substituídas respostas da API.
- Os testes de interface precisaram respeitar os elementos semânticos agrupados e a renderização sob demanda do ListView Flutter. As capturas mostram a mensagem recebida; a resposta também foi verificada no React e SQL.
- Uma execução já havia aprovado os testes Flutter reais quando a conexão HTTP ociosa do script Node expirou na limpeza lógica final da fixture. O script passou a fechar as conexões e a bateria integrada foi executada novamente com sucesso.

## Não validado / pendências externas

- TomTom real: falta chave autorizada e confirmação de plano, limites, termos e cobertura efetiva de Santos. A integração permanece desativada. O adaptador consulta um trecho por região; não fornece cobertura de todas as vias nem catálogo de incidentes.
- Google Traffic Layer foi avaliado documentalmente, sem ativação de chave ou faturamento.
- Groq real: os testes exercitaram o modo simulado identificado. Falta validar GROQ_API_KEY e modelo autorizado para chamadas reais.
- SMTP real: não foram enviados e-mails externos; o recurso anterior foi preservado.
- Android/iOS em aparelho: GPS, permissões do sistema, push (não implementado), APK e iOS não foram validados. A atualização de mensagens é periódica na tela aberta, sem promessa de notificações push.
- Leitores de tela físicos e Libras: a semântica Flutter foi exercitada no navegador. Não foi encontrada integração de Libras ativa nos arquivos inspecionados; não foi removida uma integração existente. Testes com TalkBack/VoiceOver continuam pendentes.
- Geocodificação não substitui cadastro municipal, não tem validação por polígono oficial e depende do conteúdo do OSM. Falhas permitem registrar endereço textual sem marcador.
- WASM não é suportado pelo pacote atual de armazenamento seguro; o build web validado é JavaScript.

## Evidências e reprodução

Relatórios sem credenciais: artifacts/validacao-v2.json, artifacts/validacao-admin-web-v2.json e artifacts/validacao-flutter-web-v2.json. Capturas: artifacts/v2-dashboard.png, v2-mensagens-react.png, v2-funcionarios-react.png, v2-flutter-localizacao.png, v2-flutter-detalhes.png, v2-flutter-mensagens.png e v2-flutter-transito.png.

[Execução, configurações e decisões](SANTOS-CONECTA-2.md) · [Arquivos da evolução](ARQUIVOS-V2.md).

## Registro histórico preservado

As seções abaixo descrevem versões anteriores.

# Registro de validação

> Registro histórico. A evolução posterior confirmou conexão SQL real e aplicou o script incremental. Veja [VALIDACAO-EVOLUCAO.md](VALIDACAO-EVOLUCAO.md) para os resultados atuais.

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
