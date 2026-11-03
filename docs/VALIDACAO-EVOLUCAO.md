# Validação da evolução — 31/10/2026

## Prévia Flutter web adicionada posteriormente

O mesmo aplicativo agora pode ser executado em http://127.0.0.1:5200 sem Android Studio/Android SDK. Build JavaScript web aprovado; análise sem problemas e os quatro testes Flutter passaram após a adaptação de transporte HTTP. No navegador, a sessão usa cookie HttpOnly, sem expor o ticket ao Dart ou ao localStorage. No Android o armazenamento seguro permanece.

O Chrome com a interface Flutter real confirmou cadastro, login, cookie HttpOnly, formulário nas quatro etapas, localização fictícia concedida pelo navegador e criação persistida com protocolo no SQL Server. Não houve mock da API ou do banco. O teste cria dados acadêmicos explícitos; sua localização é simulada pelo Chrome, não obtida da posição real do usuário. Capturas em `artifacts/flutter-web-*.png` e script `scripts/test-mobile-web.mjs`.

O teste de interface precisa digitar pelo teclado do navegador: substituir diretamente valores dos elementos HTML temporários do Flutter não atualizou os controllers de forma confiável. O teste foi ajustado, sem mudar as validações dos formulários.

Também foram confirmadas a presença da reclamação na conta autenticada e a manutenção da sessão após recarregar a página. A visualização em desktop mantém largura de celular.

Execução e diferenças entre web/Android: [FLUTTER.md](FLUTTER.md). A prévia local utiliza servidor estático/proxy Node, sem regras de negócio, para a mesma API C# na porta 5082.

## Validação anterior da evolução

Resultados observados no projeto real. Compilação, testes de API, testes de widgets e execução Android são verificações diferentes.

| Verificação | Resultado |
|---|---|
| Build React/TypeScript | Aprovado (`npm.cmd run build`) |
| Build C# | Aprovado, quatro projetos net8.0, 0 erros e 0 avisos |
| Conexão SQL real | Confirmada fora do sandbox, em tcp:PCEDUARDO,1433, SantosConectaDB, Windows Authentication, TLS habilitado |
| Tabelas anteriores | Confirmadas: inicialmente 24 cidadãos, 5 funcionários, 10 categorias, 132 ocorrências, 246 históricos e nenhum e-mail |
| Script incremental 03 | Aplicado; 11 indicadores fictícios e 3 categorias adicionais (as demais já existiam), sem apagar registros |
| Procedure territorial | Executada por sqlcmd, 11 linhas com TipoDado SIMULAÇÃO ACADÊMICA |
| Cadastro/login/isolamento/status/triagem/e-mail/navegador | **55 verificações aprovadas**, `node scripts/test-citizen.mjs --browser` |
| Fórmula determinística | **6 verificações aprovadas**, projeto tests/priority/PriorityChecks.csproj |
| Flutter SDK | Instalado localmente: 3.47.9, Dart 3.13.7; estrutura Android gerada pelo SDK oficial |
| Suíte Playwright anterior | 3 aprovados; 3 ignorados explicitamente por ausência de E2E_PASSWORD, sem substituir os fluxos por mocks |
| flutter analyze | Aprovado, **No issues found**, em cópia idêntica sem acentos |
| flutter test | **4 testes aprovados**, via `npm.cmd run test:mobile` |
| Android SDK | Ausente, confirmado por flutter doctor -v |
| APK/emulador/aparelho físico | Não executados; falta configurar Android SDK/toolchain/dispositivo |
| Groq real | Não testada: GROQ_API_KEY não estava configurada no processo |
| Resposta administrativa assistida | Testada no modo simulado existente; não foi apresentada como consulta real à IA |
| Classificação com IA indisponível | Testada resposta de fallback, permitindo continuar manualmente |
| SMTP real | Não testado; nenhum envio externo realizado |
| E-mail simulado e histórico SQL | Testados; StatusEnvio=Simulado, DataEnvio=NULL, sem aparecer como resposta entregue ao cidadão |

## Cobertura integrada

- Validação de cadastro, persistência, hash seguro e senha incorreta recusada.
- Dois cidadãos separados; perfil próprio atualizado.
- Cidadão bloqueado em oito rotas administrativas e na alteração de status.
- IdCidadao externo ignorado na abertura; autor obtido da sessão.
- Status e prioridade enviados pelo cidadão não substituem valores do servidor.
- Listagem por autor e 404 no detalhe de outro cidadão.
- Funcionário autenticado altera status; consulta do cidadão recebe atualização e histórico persistidos.
- Ausência de evento Em análise não fabrica essa etapa concluída.
- Revisão territorial persistida, justificativa obrigatória e histórico com autor.
- Cálculo no SQL/API: 30 gravidade + 0 espera + 16 território (índice 65) + 10 impacto = 56.
- Risco imediato sinalizado independentemente da pontuação; cálculo unitário também verifica risco com pontuação manual zero.
- Equidade consultada no banco, resolução pelo funcionário e atualização para o cidadão.
- Comunicação simulada persistida e não exposta como entrega real no aplicativo.
- Logout e consulta posterior sem sessão recusada.
- Login pelo formulário React; dashboard, lista, fila, mapa, comunicação e configurações carregados no Chrome; camada territorial rotulada e fila responsiva em 390 × 844.

Capturas em `artifacts/premium-login.png`, `premium-dashboard.png`, `premium-ocorrencias.png`, `premium-fila.png`, `premium-mapa.png`, `premium-comunicacao.png`, `premium-configuracoes.png` e `premium-fila-mobile.png`.

## Limites e correções durante a validação

O sandbox apresentou falha TLS antes de abrir master; a mesma configuração autenticou fora dele. Não foram alterados certificados, SSPI, privilégios ou políticas globais do SQL Server. O primeiro CREATE INDEX exigiu QUOTED_IDENTIFIER; o script foi corrigido e aplicado com sucesso.

O Flutter 3.47.9 apresentou falha LSP com o nome acentuado do workspace e falha nativa de shaders com o caminho acentuado do SDK. O script `scripts/check-mobile.ps1` valida fontes idênticas em diretório temporário ASCII e acessa o SDK por uma junction temporária ASCII. Não altera o SDK nem o projeto original. Uma cópia nova eliminou artefatos da tentativa de shaders anterior; análise e quatro testes passaram.

Os quatro testes Flutter verificam datas inválidas, catálogo previsto, armazenamento/expiração do ticket sem persistir senha e rejeição de confirmação divergente antes de chamar a API. Os testes de HTTP/widgets usam MockClient para isolar o cliente; os 55 checks integrados usam **API e SQL reais**, sem substituição do banco por mocks.

Uma captura teve falha transitória ao reiniciar Vite e outra excedeu a espera durante carregamento. A suíte passou após estabilizar os servidores e aguardar a resposta de login explicitamente. O cliente React agora tem limite de espera de rede de 45 segundos, compatível com os timeouts de Groq/SMTP.

As contas e reclamações criadas são fictícias, com senhas aleatórias não impressas. A fixture é Funcionário, sem ampliar privilégios; reclamações de teste foram encerradas como Cancelada e os históricos preservados. Nenhuma senha de conta existente foi alterada. Alterações Git anteriores à tarefa foram preservadas.

Ainda é necessário validar no Android: teclado, armazenamento seguro nativo, permissão GPS real, mapa e atualização após atendimento. Build/análise/testes do cliente não substituem essa execução. Também permanecem testes reais de Groq e SMTP após configurar credenciais do backend.
