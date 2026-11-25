# Santos Conecta 2.0 — decisões e execução

Projeto acadêmico, sem vínculo com a Prefeitura de Santos. Evolução incremental do React, Flutter e ASP.NET Core existentes. Não há exclusão de registros nem contratação de serviços.

## Instalação e execução

1. Configure o SQL Server conforme o README. Execute `npm.cmd run diagnosticar:sql` sob uma identidade Windows autorizada. Neste executor, o SQL funciona fora do sandbox; dentro dele ocorre erro SQL 20 de TLS. Não desative criptografia para contornar isso.
2. Sobre a estrutura existente (scripts 01–03), aplique `database/04-santos-conecta-v2.sql` e `database/05-localizacao.sql`. São repetíveis, não removem tabelas nem dados. Ambos foram aplicados no banco local nesta execução.
3. `npm.cmd install`, `dotnet restore SantosConecta.sln` e `dotnet build SantosConecta.sln --no-restore`.
4. Inicie a API: `npm.cmd run api` (porta padrão 5080). Em outro terminal: `npm.cmd run dev` (5173).
5. Para Flutter, siga `docs/FLUTTER.md`. Emulador Android: `flutter run --dart-define=API_URL=http://10.0.2.2:5080 --dart-define=ALLOW_HTTP=true`. Em produção use HTTPS. A versão web pode ser compilada com `npm.cmd run mobile:web:build` e servida com `npm.cmd run mobile:web`; o proxy agora usa a porta 5080 por padrão. Configuração atual de trânsito e notificações em `docs/TRANSITO-E-NOTIFICACOES.md`.

Não foram alteradas senhas existentes. As contas de testes usam e-mails `@exemplo.invalid`, senhas aleatórias efêmeras e dados fictícios. Não utilize essas contas como acesso permanente. O cadastro de funcionários fica no menu exclusivo do administrador.

## Ocorrências e correção do contrato

- `GET /api/ocorrencias/minhas`: todas as ocorrências do proprietário, inclusive Resolvida e Cancelada.
- `GET /api/ocorrencias/{id}`: rota canônica utilizada pelo Flutter. Para cidadão retorna `ocorrencia` e `respostas`, filtrando pelo ID da sessão. Para equipe mantém o DTO administrativo anterior.
- `GET /api/ocorrencias/minhas/{id}`: compatibilidade preservada.
- `PUT /api/ocorrencias/{id}/status`: somente equipe. O ID é o inteiro interno `idOcorrencia`, nunca o protocolo textual.

A inspeção encontrou o contrato canônico de detalhes restrito à equipe, enquanto o aplicativo dependia de rota alternativa. Não havia filtro que excluísse resolvidas no repositório atual. A correção unifica o contrato sem remover a autorização por proprietário; os testes verificam os dois caminhos e todos os estados. Isso não permite afirmar qual versão instalada anteriormente originava o 404 sem seu registro de requisição, mas evita essa divergência na versão entregue.

## Funcionários e autorização

O mesmo painel serve Administrador e Funcionário. A gestão de contas (GET da coleção completa, GET individual, POST e PUT) exige Administrador no ASP.NET Core. `/api/funcionarios/responsaveis` fornece somente identificação operacional dos ativos aos dois perfis. O menu administrativo é ocultado ao funcionário.

Cadastro/edição incluem nome, e-mail único, telefone, cargo, perfil, ativo/inativo e senha inicial. Senhas exigem 12 caracteres, maiúscula, minúscula, número e símbolo; apenas o hash é armazenado. Na edição, senha vazia mantém o hash. Não há DELETE. Uma conta não pode remover seu próprio acesso administrativo. A API revalida conta ativa e perfil no banco a cada requisição, invalidando cookies antigos quando o acesso muda. Responsáveis inativos não recebem novas atribuições.

## Conversa privada e notificações

Tabela `MensagemOcorrencia`, com ocorrência, remetente, tipo, nome histórico, conteúdo, envio e leitura. O ID do remetente é interpretado junto com o tipo, pois cidadãos e funcionários possuem tabelas próprias. O vínculo da ocorrência possui FK; nenhuma exclusão em cascata é usada.

Rotas GET/POST `/api/ocorrencias/{id}/mensagens`, POST `.../lidas` e GET `/api/notificacoes`. Todas exigem sessão. Um cidadão de outra ocorrência recebe 404. Mensagens vazias ou com marcação HTML são recusadas; os clientes renderizam texto. Uma leitura marca mensagens recebidas pelo perfil contrário, sem apagar histórico.

React e Flutter consultam a conversa a cada 15 segundos; as consultas do React pausam em segundo plano e as do Flutter respeitam o ciclo de vida do aplicativo. A central de notificações do aplicativo atualiza a cada 30 segundos. Leitura é explícita. Não há SignalR, envio externo ou chat público. A comunicação por e-mail anterior foi preservada.

## Localização

Fluxo Flutter: selecionar bairro com busca textual → rua/avenida → número opcional → Localizar endereço → confirmar alternativa → revisar e enviar. GPS permanece opcional e só é solicitado durante a ação do cidadão. Alterar bairro, rua ou número invalida o ponto anterior. Não é necessário tocar no mapa.

Catálogo transcrito das plantas municipais de abairramento, ordenado com cultura pt-BR. As fontes distinguem bairros, áreas portuárias e áreas continentais; o catálogo não contém bairros dos municípios vizinhos. Fontes:

- [Prefeitura: abairramento insular, Anexo I](https://www.santos.sp.gov.br/static/files_www/files/portal_files/Urbanismo/COINURB/luos_1006_2018_abairr_anexo_i.pdf).
- [Prefeitura: abairramento continental, Anexo IV](https://www.santos.sp.gov.br/static/files_www/files/portal_files/anexo_iv_-_abairramento.pdf).

A busca é feita no backend pelo Nominatim, sem autocomplete: a [política do serviço público](https://operations.osmfoundation.org/policies/nominatim/) proíbe autocomplete e limita o uso. Há User-Agent identificável, intervalo global mínimo de 1,1 segundo, cache em memória de 24 horas e limite de cinco alternativas. O endereço é enviado ao serviço apenas ao pressionar Localizar. `Geocoding__BaseUrl` permite trocar por outra instância compatível. O uso público deve permanecer leve; não escale o serviço público para uma implantação municipal real.

O resultado só é aceito quando há logradouro, município Santos e bairro correspondente. Quando existe número, o número retornado precisa corresponder. A equivalência é textual sem acentos; não há base municipal de polígonos nem prova cadastral de endereço. Inconsistências ou ausência de bairro no OSM resultam em nenhuma alternativa, permitindo o registro textual.

`LocalizacaoConfirmada=false` identifica endereço sem ponto validado. Para preservar as colunas decimais existentes, a ocorrência textual utiliza 0/0 como ausência técnica, **nunca como uma coordenada geográfica confirmada**. Ambos os clientes omitem o marcador e exibem a condição. Os dados anteriores preservam seus valores, com confirmação legada padrão; isso não transforma pontos demonstrativos antigos em levantamento cadastral oficial.

## Trânsito

Foram avaliados [TomTom Traffic](https://docs.tomtom.com/traffic-api/) e [Google Traffic Layer](https://developers.google.com/maps/documentation/javascript/trafficlayer). A alternativa preparada é TomTom por HTTP no backend, consumida igualmente por Leaflet e flutter_map, sem substituir a base OpenStreetMap. Google exigiria outro SDK/mapa para Flutter e configuração de chave/faturamento.

Por padrão não há chamadas ao TomTom, mesmo que uma chave exista. As telas exibem **Dados de trânsito indisponíveis**, mapa de referência, regiões, legenda prevista e ausência de última atualização. Para habilitação futura, após conferir plano, cobertura, limites e autorização de uso:

```powershell
$env:Traffic__Enabled = 'true'
$env:TOMTOM_API_KEY = 'configure-somente-no-servidor'
```

O adaptador usa [Flow Segment Data](https://docs.tomtom.com/traffic-api/documentation/tomtom-maps/v1/traffic-flow/flow-segment-data), com cache de um minuto por região. A chave permanece no backend e o logger HTTP dessa integração é removido para não registrar a URL com chave. A resposta contém coordenadas e velocidades reais do **trecho mais próximo do ponto de referência**, e não uma cobertura completa da cidade. Apenas essas coordenadas são desenhadas. Verde/amarelo/vermelho representam faixas da razão velocidade atual/livre; cinza representa interdição informada pelo provedor. O horário mostrado é a consulta ao provedor, não uma garantia do instante de medição.

Incidentes detalhados ainda não estão integrados e são explicitamente indisponíveis. Não há scraping nem congestionamentos inventados. A cobertura efetiva de Santos, cores com dados reais, limites e custos não foram validados sem chave. Confira a [página de preços atual](https://docs.tomtom.com/pricing); não foi contratado plano nem presumida gratuidade ilimitada. As atribuições de OpenStreetMap e do trânsito TomTom foram preservadas nas interfaces.

## Dashboard e IA

O dashboard usa registros reais do SQL (a base acadêmica contém dados fictícios), com filtros por período, status e prioridade; separa abertas, em análise, em andamento e resolvidas. Atraso significa ocorrência não finalizada há mais de sete dias, **regra acadêmica, sem SLA oficial**. A média utiliza apenas resolvidas com evento registrado de resolução, em horas entre abertura e último evento de resolução. Não estima datas ausentes. Conversas pendentes contam ocorrências com mensagem de cidadão posterior à última resposta da equipe. Distribuições, mapa e fila foram preservados, sem recriar Relatórios.

Groq continua exclusivamente no backend com `GROQ_API_KEY` configurada no `.env` da raiz. Na conversa, o funcionário pode sugerir resposta, resumir histórico, sugerir classificação ou identificar informações faltantes. As saídas são rascunhos; nenhuma altera categoria, status ou envia mensagem automaticamente.

## Testes reproduzíveis

```powershell
npm.cmd run build
dotnet build SantosConecta.sln --no-restore
npm.cmd run test:mobile
# API de validação já iniciada com SQL real:
$env:API_TEST_URL='http://127.0.0.1:5086'
$env:WEB_TEST_URL='http://127.0.0.1:5178'
node scripts/test-citizen.mjs
node scripts/test-v2.mjs --flutter
# Após compilar e servir Flutter web na porta 5200:
node scripts/test-mobile-web-v2.mjs
```

Os testes criam fixtures novas; não alteram contas reais. `--flutter` executa o runner com HTTP/SQL reais, usando os serviços Dart e substituindo apenas o armazenamento seguro local do dispositivo no ambiente sem aparelho. A bateria de interface Flutter web é separada. Builds JavaScript do Flutter funcionam; o dry-run de WebAssembly informa incompatibilidade do pacote de armazenamento seguro, então não há suporte WASM validado. APK, iOS, GPS em aparelho e leitores de tela físicos permanecem fora da validação deste executor.

Consulte `docs/VALIDACAO.md` para resultados efetivamente executados, falhas intermediárias corrigidas e pendências externas.
