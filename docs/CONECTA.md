# Conecta — assistente do cidadão

## Entrega

Conecta está disponível na navegação do Flutter e no portal React `/cidadao`. O painel administrativo mantém o assistente anterior e não recebe o chat flutuante do cidadão. O novo portal tem login com a conta cidadã existente, consulta dos próprios protocolos e suas mensagens, clima, avisos e o mapa existente. Há um acesso ao portal na tela de login da equipe.

O Flutter oferece tela de conversa com sugestões, mensagens, rolagem, indicação de consulta em andamento e limpeza de histórico. O React oferece botão flutuante e janela com minimizar, fechar, sugestões, teclado, histórico e ações. As mensagens são texto, sem execução de HTML ou Markdown fornecido pelo modelo.

## Como abrir

Reinicie a API no terminal em que ela já funciona:

```powershell
# Ctrl+C no processo anterior; depois:
npm.cmd run api
```

Com o painel em execução (`npm.cmd run dev`), abra `http://localhost:5173/cidadao` e entre com uma conta de cidadão, não de funcionário. A conta pode ser criada pelo aplicativo Flutter existente.

O Flutter web foi reconstruído durante a entrega. Com o servidor existente `npm.cmd run mobile:web`, abra `http://127.0.0.1:5200`, atualize com Ctrl+F5 e use **Conecta** na navegação inferior. Não inicie outro servidor na mesma porta se já estiver funcionando.

O chat não exige tabelas novas. Continuam necessárias as tabelas existentes para reclamações, mensagens e meteorologia. Se a instalação ainda não aplicou as atualizações anteriores, execute `npm.cmd run db:update` em seu PowerShell com acesso ao SQL, antes de reiniciar a API. O chat trata falhas dessas fontes sem inventar dados.

## IA e modo orientativo

Foi reutilizado o provedor Groq já presente no projeto. A integração nova é separada das sugestões administrativas, preservando suas configurações e comportamentos. A IA interpreta a pergunta em linguagem natural e referências do contexto como “esse protocolo” e “e amanhã?”. Retorna um plano JSON com uma intenção permitida e parâmetros limitados. O backend valida esse plano e produz as respostas factuais a partir dos serviços autorizados.

O modelo não recebe dados do banco, nomes do perfil, endereços das reclamações, cookies ou chaves. Recebe somente a pergunta e até oito mensagens anteriores do cidadão, com omissão de e-mails e telefones reconhecidos. Não devolve texto livre de fatos para exibição: os dados de status, previsão, horários e ações são montados pelo backend. Isso reduz o risco de fatos inventados ou instruções maliciosas contidas no histórico.

Sem chave, com IA desativada, cota atingida, timeout, erro do provedor ou plano inválido, o sistema usa reconhecimento local de intenções conhecidas. A resposta aparece como **modo orientativo — IA indisponível ou não utilizada**. Esse fallback tem compreensão mais limitada que um modelo de linguagem; perguntas fora do escopo recebem orientação explícita.

Nenhuma credencial Groq foi encontrada na configuração deste ambiente. Portanto não foi validado o provedor real nesta entrega. Para ativar no terminal da API:

```powershell
$env:GROQ_API_KEY = '<sua chave privada, somente no servidor>'
$env:Conecta__AIEnabled = 'true'
$env:Conecta__Model = 'openai/gpt-oss-20b'
npm.cmd run api
```

Não incluir a chave no React, Flutter, Git ou capturas de tela. `conecta.settings.example.json` é somente referência; não é carregado automaticamente. Configure por variáveis ou pelo provedor local já usado pela API. O modo Conecta não depende de `Groq:Simular`, que continua controlando o assistente administrativo anterior.

Limites padrão: 40 chamadas de IA por cidadão/dia UTC, 500 globais/dia UTC, uma chamada por pergunta, saída de até 450 tokens e timeout externo de 12 segundos. `Conecta__MaxPerUserDay` e `Conecta__MaxPerDay` configuram os limites dentro dos tetos de 100 e 5.000. Os contadores são em memória; reiniciar ou escalar a API reinicia/distribui as cotas. Para uma produção com múltiplas instâncias, será necessário um contador compartilhado ou limite no provedor. As cotas não impedem o modo orientativo.

## Dados e autorização

O endpoint aceita somente o texto da pergunta. A identidade vem das claims da sessão cidadã, nunca de um campo `idCidadao` recebido no corpo. As consultas usam as mesmas fontes SQL/serviços dos endpoints existentes, sem fazer uma chamada HTTP da API para ela própria:

- Reclamações: projeção de ID, protocolo, categoria, status e data, filtrada por cidadão **antes** da projeção. Até oito resultados por resposta. Os registros completos continuam na lista existente.
- Respostas: somente mensagens da equipe vinculadas a uma ocorrência desse mesmo cidadão; informa quantidade e última data, com botão para abrir o protocolo.
- Protocolo inexistente ou pertencente a outra pessoa: mesma resposta de “não encontrado na sua conta”, sem confirmar a existência de registro alheio.
- Clima: `ClimaService`/Open-Meteo, com horários, data de previsão, cache e indicação de estimativa de modelo. Dados nulos não são substituídos por números fictícios.
- Alertas: base de avisos ativos e não expirados; distingue `Automatico` de origem oficial. A ingestão oficial continua com as limitações documentadas na entrega meteorológica; Conecta não cria alertas.
- Trânsito: `TransitoService`/TomTom nos pontos de referência de Ana Costa ou Conselheiro Nébias. Uma consulta de trecho não é apresentada como cobertura de toda a avenida. Dados indisponíveis não significam trânsito livre.

O assistente não cria reclamações, não envia mensagens à equipe, não marca leitura, não executa SQL, não muda permissões e não envia notificações. O botão de nova reclamação abre o formulário existente, cuja revisão e envio continuam com o cidadão.

## Ações de navegação

| ID permitido | Destino |
|---|---|
| `NOVA_RECLAMACAO` | Formulário Flutter existente |
| `MINHAS_RECLAMACOES` | Lista pessoal |
| `DETALHE_RECLAMACAO` | Detalhe de uma ocorrência retornada pela consulta autorizada |
| `MENSAGENS` | Lista de protocolos para escolher a conversa |
| `CLIMA` | Clima em Santos |
| `ALERTAS` | Central de Alertas |
| `TRANSITO` | Mapa; somente as duas avenidas permitidas podem ser pré-selecionadas |

Os clientes mapeiam esses IDs localmente. O React ignora IDs desconhecidos; o Flutter usa um switch fechado. Não há execução de URLs retornadas pela IA.

No React, registrar uma reclamação encaminha ao aplicativo Flutter existente. Em desenvolvimento, o destino é o mesmo hostname na porta 5200 e `?acao=nova`; o Flutter abre o formulário depois de autenticar. Em produção, configurar **`VITE_CITIZEN_APP_URL`** com o endereço da implantação Flutter; sem configuração o destino fixo é `/aplicativo/`, cuja hospedagem precisa existir. Essa é uma configuração do projeto, nunca uma URL recebida do modelo.

## Endpoints

Todos exigem papel `Cidadao`; o frontend envia `X-Santos-Client: citizen` para selecionar o cookie cidadão separado do administrativo.

| Método | Endpoint | Contrato |
|---|---|---|
| POST | `/api/conecta/mensagens` | `{ "mensagem": "Vai chover amanhã em Santos?" }`; retorna papel, texto, data UTC, modo e ações |
| GET | `/api/conecta/historico` | mensagens e texto explicativo da retenção |
| DELETE | `/api/conecta/historico` | limpa somente o contexto da conta autenticada; 204 |

Limites: 1.000 caracteres de entrada, corpo HTTP de até 8 KiB, resposta armazenada de até 6.000 caracteres e 25 segundos para a operação completa. Rate limiting de 12 requisições por minuto por cidadão, incluindo histórico e limpeza; excesso retorna 429. O middleware de autenticação precede o limiter para a chave usar a identidade validada. Chamadas anônimas e administrativas são recusadas. Erros de fonte retornam orientação sem inventar fatos; timeout total retorna mensagem 503 amigável.

## Histórico e privacidade

Não foi criada tabela de conversa. A opção adotada é memória temporária no backend: até 20 mensagens (perguntas e respostas), limite de mil conversas residentes, validade máxima de 24 horas e expiração após 30 minutos sem uso. Reinício ou pressão no limite de memória pode encerrar o histórico. A interface informa essa limitação. Isso oferece contexto básico sem persistir indefinidamente perguntas pessoais no banco acadêmico.

O histórico é isolado pela identidade, serializado por conversa para evitar conflitos e pode ser limpo pela conta. Não é armazenado em localStorage. O modelo não escolhe a identidade, o SQL, o endpoint ou o destino. Tentativas explícitas de comandos/segredos são recusadas; formatos comuns de chaves e menções a credenciais não são gravados ou encaminhados. A filtragem não substitui o cuidado do cidadão ao escrever dados pessoais. Logs de chamadas ao provedor foram removidos; o corpo das conversas não é registrado pelo serviço. Limpar remove o contexto mantido pelo Santos Conector, não altera eventuais políticas de retenção do provedor externo.

## Validação

- API compilada em diretório separado, preservando a instância eventualmente aberta pelo usuário.
- 20 verificações determinísticas do Conecta: modo sem chave sem chamada externa; criação somente pelo formulário; filtro de abertas; protocolo de outro cidadão; respostas autorizadas; clima e contexto de amanhã; trânsito indisponível; identificação de aviso automático; falha de fonte; segredos e prompt injection; histórico isolado, limitado e limpável; validação de entrada; rejeição de intenções/URLs do modelo; cota e indisponibilidade do provedor.
- 15 verificações HTTP no host de teste: sessões separadas; cidadão autorizado; equipe/anônimo recusados pelo controller real; envio/resposta; entrada longa recusada; limpeza e rate limiting. O host usa identidades e consultas de fixture, sem banco real.
- Testes existentes: seis verificações de prioridade e dez de clima/geometria passaram.
- React/TypeScript/Vite: build aprovado. Chrome com fixtures identificadas: envio e resposta, indicação de fallback, ação desconhecida ignorada, detalhe do protocolo, clima, trânsito com avenida pré-selecionada, minimizar, fechar, limpar, telas de 390 e 1.440 px. Há teste para o destino fixo do formulário existente. Capturas em `artifacts/conecta-fixture-mobile.png` e `conecta-fixture-desktop.png`.
- Flutter: analyze sem problemas; dez testes passaram (incluem quatro do Conecta para envio/resposta/limpeza e ações de reclamações, trânsito e clima). Um teste de integração SQL foi ignorado explicitamente por exigir fixture e credenciais.
- Flutter web: compilação aprovada pelo script existente. Houve avisos de incompatibilidade Wasm do `flutter_secure_storage_web` e fonte Cupertino já presentes, sem impedir o build JavaScript. A validação não inclui APK/Android físico.
- `dotnet test SantosConecta.sln --no-restore`: concluiu, mas a solução não contém testes descobertos pelo runner; as verificações efetivas foram os projetos executáveis descritos acima. A suíte Playwright completa que depende de SQL real não foi executada.

Os testes de IA usam respostas de fixture, jamais apresentadas como dados reais fora dos testes. A chave está ausente e não houve chamada real ao modelo. Não foi validada a conversa ponta a ponta com SQL real nesta sessão. As consultas externas reais de clima e trânsito também não foram revalidadas como parte dos testes do chat; foram validadas com fontes de fixture e tratamento de falhas. O NuGet pode emitir NU1900 quando a consulta externa de vulnerabilidades estiver bloqueada; isso não comprova a execução dessa auditoria.

Reprodução:

```powershell
dotnet build backend/SantosConecta.API --no-restore
dotnet run --project tests/conecta
dotnet run --project tests/sessions
dotnet run --project tests/priority
dotnet run --project tests/weather
npm.cmd run build
# Com preview React na porta 5177:
node scripts/test-conecta-ui.mjs
npm.cmd run test:mobile
npm.cmd run mobile:web:build
```

## Arquivos

Criados:

- `backend/SantosConecta.Infrastructure/Services/ConectaContracts.cs`
- `backend/SantosConecta.Infrastructure/Services/ConectaConsultas.cs`
- `backend/SantosConecta.Infrastructure/Services/ConectaModelo.cs`
- `backend/SantosConecta.Infrastructure/Services/ConectaService.cs`
- `backend/SantosConecta.API/Controllers/ConectaController.cs`
- `backend/SantosConecta.API/conecta.settings.example.json`
- `src/services/conecta.ts`
- `src/components/ConectaChat.tsx`
- `src/pages/CitizenPortal.tsx`
- `src/styles-conecta.css`
- `mobile/lib/screens/conecta_screen.dart`
- `mobile/test/conecta_test.dart`
- `tests/conecta/ConectaChecks.csproj` e `Program.cs`
- `scripts/test-conecta-ui.mjs`
- `docs/CONECTA.md`
- Capturas de teste identificadas `artifacts/conecta-fixture-*.png`

Modificados: `backend/SantosConecta.API/Program.cs` (DI e limiter), `tests/sessions/Program.cs`, `src/routes/index.tsx`, `src/pages/Login.tsx`, `src/pages/TrafficPage.tsx`, `mobile/lib/screens/home_screen.dart`, `mobile/lib/screens/traffic_screen.dart`, `README.md` e `docs/FLUTTER.md`. Não há novas dependências ou alterações de banco para o Conecta.

Referência técnica consultada: [Groq — structured outputs e JSON Object Mode](https://console.groq.com/docs/structured-outputs).
