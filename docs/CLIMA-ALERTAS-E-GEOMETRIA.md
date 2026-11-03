# Clima, avisos meteorológicos e geometria de vias

Implementação de 02/11/2026. As alterações anteriores existentes no workspace foram preservadas. Não houve publicação, seed, limpeza ou alteração destrutiva no banco.

## Recursos implementados

- Flutter: Clima em Santos, condição atual, sensação térmica, umidade, vento, probabilidade horária de precipitação, próximas 24 horas, sete dias e horário da consulta. Dados ausentes aparecem como indisponíveis. As condições atuais são estimativas de modelo da Open-Meteo, não leituras de uma estação municipal.
- Central de Alertas: avisos ativos, histórico paginado, detalhes, origem, fonte, severidade, validade, região, referência e preferências de push. Banner na página inicial com consulta a cada cinco minutos.
- Backend: consulta meteorológica com cache de 15 minutos; processo independente do aplicativo a cada 15 minutos; persistência e encerramento de avisos; reservas duráveis de entrega; envio FCM HTTP v1 condicionado à configuração e consentimento.
- Web e Flutter: busca de via pelo nome completo, seleção automática do traçado nas opções de avenidas existentes, múltiplos segmentos, enquadramento, troca e limpeza. Os pontos de cada way são preservados; não se unem extremidades de segmentos desconectados com linhas artificiais.
- A camada de geometria permanece independente do fluxo e dos incidentes existentes. O traçado neutro tem espessura maior que o fluxo sobreposto, mantendo o destaque visível.

## Endpoints autenticados

| Método | Caminho | Comportamento |
|---|---|---|
| GET | `/api/clima` | Snapshot atual e previsões, fonte e validade do cache |
| GET | `/api/clima/previsao` | Mesmo snapshot, incluindo blocos hourly e daily |
| GET | `/api/clima/alertas` | Ativos e ainda não expirados; pode incluir previsão futura |
| GET | `/api/clima/alertas/historico?pagina=1` | 50 registros por página, ativos e encerrados |
| GET | `/api/clima/alertas/{id}` | Detalhes de um aviso |
| GET | `/api/clima/preferencias` | Preferência da conta cidadã; não retorna token |
| PUT | `/api/clima/preferencias` | `{ "habilitada": true, "token": "token-do-dispositivo" }` ou `{ "habilitada": false }`; apenas cidadão |
| GET | `/api/transito/geometria?nome=Avenida%20Ana%20Costa` | Nome, fonte e segmentos com latitude/longitude |
| GET | `/api/transito?regiao=Av.%20Ana%20Costa` | Endpoint existente preservado: fluxo e incidentes disponíveis |

Nenhum endpoint aceita um ID de cidadão enviado pelo cliente para preferências: utiliza a identidade autenticada. Uma conta mantém um dispositivo ativo; cadastrar outro substitui o anterior. Tokens transferidos entre contas são desassociados da conta anterior.

## Banco e ativação do serviço

Aplicar `database/07-meteorologia.sql` após os scripts anteriores, no SQL Server configurado. O script cria apenas tabelas ausentes:

- `AlertaMeteorologico`: identidade do aviso, fonte, conteúdo, região e intervalo de validade.
- `PreferenciaMeteorologica`: consentimento, token opcional e última alteração, com FK para cidadão.
- `EntregaMeteorologica`: chave composta aviso/cidadão, reserva, estado e confirmação do envio. A chave impede duplicação persistente.

O script não foi aplicado nesta sessão: a conexão SQL falhou antes de abrir o banco. Após o usuário iniciar o SQL, os serviços `MSSQLSERVER` e `MSSQL$MSSQLSERVER01` foram confirmados como Running. A nova tentativa pela API, pelo diagnóstico e pelo sqlcmd (TCP e memória compartilhada) continuou falhando com erro de criptografia/SSPI: “Credenciais não disponíveis no pacote de segurança”. A correção depende de executar em contexto Windows com acesso às credenciais e ao TLS; não foi desativada a criptografia.

Configuração de referência: `backend/SantosConecta.API/weather.settings.example.json`. Esse arquivo é documentação e não é carregado automaticamente. Configure no provedor já existente ou por variáveis de ambiente:

```text
Weather__WorkerEnabled=true
Weather__PushEnabled=false
Weather__FirebaseServiceAccountPath=<caminho protegido no servidor>
Traffic__Enabled=true
TOMTOM_API_KEY=<chave somente no servidor>
```

Habilitar Worker somente após aplicar o SQL. Por padrão, worker e push permanecem desligados. A consulta de clima e de geometria não depende das novas tabelas. Não colocar a conta de serviço no aplicativo, frontend, repositório ou diretório público. O processo não imprime chaves, tokens ou conteúdo das credenciais nos logs de envio.

## Regras preventivas e ciclo de vida

Regras v1, para o ponto de previsão de Santos (-23.9608, -46.3336), usando as horas futuras até 24 horas:

| Regra | Limiar | Identificação |
|---|---|---|
| Precipitação horária prevista | pelo menos 20 mm em uma hora | Aviso preventivo automático de chuva intensa |
| Rajada prevista a 10 m | pelo menos 60 km/h | Aviso preventivo automático de rajadas fortes |

Esses limiares são critérios de triagem do projeto, não níveis oficiais de risco. A chuva horária da Open-Meteo acumula na hora anterior ao timestamp, e o intervalo do aviso considera isso. Dados nulos não disparam regras. Nenhuma regra infere alagamento, emergência, risco por bairro ou alerta oficial. Não há classificação de tempestade automática a partir apenas de um ícone climático.

A identidade determinística inclui versão, tipo e hora da previsão. Atualizações ajustam a descrição; previsões futuras retiradas pelo modelo são encerradas. Expirados deixam a consulta ativa mesmo se o worker estiver temporariamente parado. Histórico permanece no banco. Uma falha externa não fabrica nem encerra avisos com base em ausência de resposta.

Antes de enviar, o worker verifica validade, consentimento atual e entrega já registrada. A reserva é gravada antes da chamada ao FCM. Em falha de rede ou resultado ambíguo, `FalhaOuIncerta` não é reenviada automaticamente, priorizando evitar duplicatas; uma falha pode portanto deixar de entregar. Não há garantia de entrega exatamente uma vez pelo dispositivo. A mesma identidade não gera nova notificação a cada consulta ou revisão. Não são enviadas notificações de teste pelo worker.

## Fontes oficiais: limitação explícita

Não foi ativada ingestão de alertas oficiais. Há divulgação pública do INMET e referências a RSS/CAP, mas não foi validada nesta sessão uma integração que forneça contrato, situação, validade e recorte geográfico confiáveis para Santos. Uma correspondência textual com “São Paulo” ou “Santos” em RSS não foi tratada como comprovação de cobertura municipal. A consulta ao RSS retornou conteúdo não interpretável pelo navegador de pesquisa utilizado; isso não significa que o serviço oficial inexista.

O modelo de persistência e a interface distinguem `Automatico` de uma origem oficial, mas os registros criados por este código são exclusivamente automáticos. Não existe importação manual genérica ou endpoint público de criação de “alerta oficial”. Integrar INMET/Defesa Civil exigirá validar a fonte, cobertura, referências e cancelamentos antes de habilitar o adaptador. Não há alegação de monitoramento completo de alertas oficiais.

## Push e plataformas

Android: adicionar a configuração do aplicativo Firebase em `mobile/android/app/google-services.json` (ignorada no Git), correspondente ao applicationId existente. O plugin Google Services só é aplicado quando o arquivo existe. Compilar com `--dart-define=FIREBASE_ENABLED=true`. A configuração nativa permite inicialização pelo Android em segundo plano; não há chave de serviço nem chave da TomTom no Dart.

No servidor, fornecer conta de serviço com permissão de envio FCM HTTP v1 para o mesmo projeto. Após validar em projeto Firebase e dispositivo de desenvolvimento exclusivos, habilitar `Weather__PushEnabled=true`. A confirmação local do envio significa aceitação pelo FCM, não confirmação de leitura do cidadão.

O aplicativo solicita permissão ao ativar notificações; restaura preferência e renova token ao recuperar uma sessão autorizada. Ao tocar no push, abre a Central de Alertas; em primeiro plano a chegada também abre a central. Detalhes são acessíveis pela lista. O payload contém título e dados para o sistema exibir a notificação com o aplicativo em segundo plano. Restrições do Android, falta de Google Play Services e force-stop podem impedir entrega. Sair tenta desativar a preferência; sem rede, a revogação no servidor precisa ser repetida no próximo acesso.

Push Flutter Web está explicitamente indisponível nesta implementação (não foi criado service worker/VAPID); clima, mapa e alertas funcionam via HTTP. O repositório não tem projeto iOS; APNs e compilação iOS não foram implementados nem testados. Não foi executada compilação Android/APK ou recebimento em dispositivo físico nesta sessão.

## Geometria e critérios de trânsito

Overpass consulta todos os ways highway com nome correspondente na área administrativa IBGE 3548500. `Av.` é normalizado para `Avenida`. A consulta exige o nome completo do cadastro OSM; não é pesquisa aproximada. Cache de 24 horas e exclusão mútua evitam consultas simultâneas idênticas. Falha ou nome ausente exibe erro; não há linha fictícia ou rota calculada como fallback.

A TomTom existente fornece Flow Segment Data próximo ao ponto da região e incidentes do recorte. Não se extrapola o fluxo desse ponto para a avenida inteira. As cores aplicam-se somente à geometria efetivamente retornada pela TomTom: razão velocidade atual/livre >= 0,75 verde; >= 0,35 amarelo; abaixo disso vermelho. Velocidade livre inválida ou informação ausente é cinza. Interdição retornada pelo provedor também é cinza e identificada no detalhe. São critérios de exibição do projeto, não níveis oficiais da TomTom. Trechos de avenida sem fluxo associado permanecem neutros/desconhecidos.

Foram preservados cooldown, orçamento e política existente de não armazenar respostas TomTom. O cache novo é somente meteorológico e de geometria OSM. Atribuições dos provedores permanecem visíveis.

## Validação e reprodução

As fixtures abaixo existem somente nos testes; não são fallback de produção.

| Validação | Resultado nesta sessão |
|---|---|
| `dotnet build SantosConecta.sln --no-restore -v quiet` | Sucesso, inclusive após as alterações finais: zero avisos e erros |
| `dotnet test SantosConecta.sln --no-restore -v quiet` | Concluiu; solução não contém suíte de testes descoberta pelo runner |
| `dotnet run --project tests/priority --no-restore` | 6 verificações existentes aprovadas |
| `dotnet run --project tests/weather --no-restore --no-build` | 8 verificações aprovadas: limiares, nulos, janela, IDs, geometria/cache, entrada inválida e push desabilitado sem HTTP |
| `dotnet run --project tests/weather -- --live` | Parte determinística passou; chamada real falhou em TLS/SSPI antes do clima |
| `node scripts/check-weather-sources.mjs` | Open-Meteo e Overpass: fetch failed no ambiente restrito |
| `npm.cmd run build` | React/TypeScript/Vite compilados |
| `node scripts/test-road-geometry-ui.mjs` | Chrome com fixtures: Ana Costa, dois segmentos, ponto de curva, troca por Conselheiro Nébias, limpar e largura de 390 px aprovados; sem erro JS |
| Flutter analyze | Passou após correção de avisos de estilo |
| Flutter test | 6 passaram; 1 integração com SQL ignorada por exigir fixture/credenciais |
| Flutter build web | Compilou; avisos de incompatibilidade Wasm do secure_storage existente e fonte Cupertino, sem impedir build JS |
| HTTP sem autenticação | Novos endpoints retornaram 401 como esperado |
| `/health` após nova tentativa SQL | 503; serviços SQL ativos, falha SSPI/criptografia do cliente |
| Push real | Não executado; nenhuma notificação enviada |

Os comandos Flutter existentes usam cópia temporária e junction do SDK para contornar caminhos Windows com acentos. Foi necessário direcionar APPDATA/LOCALAPPDATA para `.flutter-home` gravável na sessão de validação. Invocar o SDK diretamente pelo caminho com acentos fez um hook nativo falhar; o script com junction funcionou.

O projeto de verificações meteorológicas emitiu NU1900 ao tentar consultar o índice de vulnerabilidades NuGet sem acesso à rede. Os testes determinísticos passaram; a consulta de vulnerabilidades não foi concluída.

Pendências de validação externa: aplicar script no SQL acessível, testar persistência e consentimento com sessão real, consultar Ana Costa/Open-Meteo pela API em ambiente com HTTPS funcional, testar atualização e retirada de previsões, receber FCM em dispositivo de desenvolvimento com aplicação em segundo plano e executar a suíte completa com SQL. A renderização aprovada com fixtures não comprova a geometria real da avenida nem dados meteorológicos atuais.

## Arquivos desta implementação

Criados:

- `backend/SantosConecta.Domain/Entities/AlertaMeteorologico.cs`
- `backend/SantosConecta.Infrastructure/Services/ClimaService.cs`
- `backend/SantosConecta.Infrastructure/Services/ViaService.cs`
- `backend/SantosConecta.Infrastructure/Services/MeteorologiaWorker.cs`
- `backend/SantosConecta.API/Controllers/ClimaController.cs`
- `backend/SantosConecta.API/weather.settings.example.json`
- `database/07-meteorologia.sql`
- `mobile/lib/screens/weather_screen.dart`
- `mobile/lib/screens/weather_alerts_screen.dart`
- `mobile/lib/services/weather_push.dart`
- `mobile/lib/widgets/weather_alert_banner.dart`
- `mobile/test/weather_test.dart`
- `tests/weather/WeatherChecks.csproj` e `Program.cs`
- `scripts/check-weather-sources.mjs`
- `scripts/test-road-geometry-ui.mjs`
- `docs/CLIMA-ALERTAS-E-GEOMETRIA.md`
- `artifacts/geometry-fixture-desktop.png` e `geometry-fixture-mobile.png` (capturas de teste identificadas)

Modificados a partir do estado encontrado:

- `backend/SantosConecta.API/Program.cs`
- `backend/SantosConecta.API/Controllers/TransitoController.cs`
- `backend/SantosConecta.Infrastructure/Data/SantosConectaDbContext.cs`
- `src/pages/TrafficPage.tsx`
- `mobile/lib/screens/traffic_screen.dart`, `home_screen.dart` e `mobile/lib/main.dart`
- `mobile/pubspec.yaml` e `pubspec.lock`
- `mobile/android/settings.gradle.kts`, `app/build.gradle.kts` e `app/src/main/AndroidManifest.xml`
- `.gitignore`

## Referências técnicas

- [Open-Meteo Forecast API](https://open-meteo.com/en/docs): variáveis, unidades, códigos WMO e atribuição; uso gratuito não comercial adequado ao projeto acadêmico, sujeito aos termos e limites do provedor.
- [Overpass: geometria OSM](https://dev.overpass-api.de/overpass-doc/en/full_data/osm_types.html): `out geom`.
- [TomTom Flow Segment Data](https://docs.tomtom.com/traffic-api/documentation/tomtom-maps/v1/traffic-flow/flow-segment-data): segmento próximo ao ponto e velocidades disponíveis.
- [Firebase: recebimento no Flutter](https://firebase.google.com/docs/cloud-messaging/flutter/receive-messages) e [configuração Android](https://firebase.google.com/docs/android/setup).
- [WMO: autoridade INMET](https://alertingauthority.wmo.int/authorities.php?recId=22): referência da autoridade e divulgação CAP; não comprova integração ativa do Santos Conector.
