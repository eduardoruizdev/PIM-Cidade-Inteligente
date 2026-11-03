# Travessia Santos–Guarujá — relatório de implementação

Pesquisa realizada em 03/11/2026. Resultado: **consulta oficial assistida implementada; monitoramento em tempo real não concluído**.

## Fontes e evidências

| Fonte | Dados visíveis | Acesso e conclusão |
| --- | --- | --- |
| [SEMIL / Departamento Hidroviário](https://semil.sp.gov.br/travessias/travessias-automoveis/santos-guaruja/) | HTML com espera nos dois sentidos, embarcações e aviso operacional. A leitura da página não trouxe data completa da medição. | A própria página avisa que o sistema está com instabilidade e as informações estão desatualizadas. Direciona para 0800 77 33 711, opção 0. Fonte oficial selecionada **para navegação**, não para coleta. |
| [METROA](https://metroa.com.br/santos/balsas/santos-guaruja) | HTML renderizado com os dois sentidos, hora de atualização, embarcações, operação e histórico de seis horas. | Declara Departamento Hidroviário como fonte e coleta própria METROA. É terceiro. Não foram localizadas documentação de API, licença de reutilização, autorização para extração ou limites publicados. Hora da coleta não comprova hora da medição original. |
| [SEMIL ArcGIS Travessias](https://mapas.semil.sp.gov.br/server/rest/services/Hosted/Travessias/FeatureServer?f=jsapi) | Serviço geográfico encontrado na pesquisa. | Não foi possível confirmar campos de espera, timestamps ou frequência; não adotado como feed operacional. |

As páginas foram consultadas pela ferramenta de pesquisa web. Leituras HTTP simples via Node `fetch`, com timeout de 20 segundos, falharam para ambas as páginas e para os dois `robots.txt` neste ambiente (`fetch failed`). O acesso a robots pela pesquisa também não foi possível. Não foi possível inspecionar os scripts/requisições de atualização, validar um feed JSON/XML, nem confirmar condições de automação. Ausência de documentação encontrada **não prova inexistência de API**; acesso público e robots permissivo também não equivalem a autorização. Não houve contorno de restrições ou consulta a endpoints privados.

## Comportamento entregue

- Card na navegação inicial existente, com ícone de embarcação; tela Flutter usa o tema global, inclusive modo escuro.
- Dois sentidos, espera indisponível, situação não confirmada e atualização original não informada.
- Consulta à API, carregamento, erro, tentativa manual e link oficial que permanece acessível mesmo sem API.
- Atualização da disponibilidade da integração a cada cinco minutos, somente com a tela atual e aplicativo em primeiro plano; nova consulta ao retomar. Timer cancelado ao fechar e chamadas simultâneas evitadas.
- API autenticada, serviço singleton com resposta imutável compartilhada, nenhuma requisição externa em execução.
- Horários de coleta e medição nulos: o horário da requisição interna não é apresentado como atualização da fonte.
- Sem medições, não há dados antigos a mostrar, gráficos, coleta periódica externa, persistência SQL ou alertas. Infraestrutura FCM existente foi avaliada, mas não gera alertas de balsa sem dados verificados.

## Endpoints

`GET /api/travessias/santos-guaruja`

Retorna `modo=consulta_oficial`, fonte, URL, aviso, intervalo de 300 segundos, embarcações nulas, comunicados vazios e dois sentidos com `disponivel=false`. `desatualizado=false` não indica atualidade: não existe medição; `disponivel` é obrigatório para interpretar esse estado.

`GET /api/travessias/santos-guaruja/historico?periodo=horas|hoje|7dias`

Retorna lista vazia e `suficiente=false`. Períodos inválidos são rejeitados. A tela apresenta ausência de histórico, sem filtros vazios nem gráfico fictício.

## Arquivos

- `backend/SantosConecta.Infrastructure/Services/TravessiaService.cs`
- `backend/SantosConecta.API/Controllers/TravessiasController.cs`
- `backend/SantosConecta.API/Program.cs` (registro no contêiner)
- `mobile/lib/screens/ferry_screen.dart`
- `mobile/lib/screens/home_screen.dart` (acesso na navegação)
- `mobile/test/ferry_test.dart`
- `tests/ferry/FerryChecks.csproj` e `Program.cs`
- `tests/sessions/Program.cs` (verificação HTTP autenticada e anônima)
- Este relatório.
- `README.md` (acesso ao relatório e comando de teste).

## Evolução necessária para tempo real

Confirmar uma fonte acessível com permissão de automação, contrato de dados e horário original verificável. Substituir `TravessiaService` e adaptar a tela para consumir medições. Só então implementar parser/validação, worker com frequência permitida, cache com expiração e falhas, histórico SQL com deduplicação/retenção e alertas opcionais. Não foi criada tabela sem dados reais para armazenar.

A frequência real de atualização das fontes permanece **não verificada**. Os cinco minutos referem-se exclusivamente à consulta do aplicativo à API interna. A leitura automática, extração e cenários de medição desatualizada não podem ser considerados testados ou concluídos nesta entrega.

## Validação executada

| Comando / verificação | Resultado |
| --- | --- |
| `dotnet build SantosConecta.sln --no-restore` | Aprovado, zero erros e avisos. |
| `dotnet run --project tests/ferry` | Nove verificações aprovadas: ambos os sentidos, ausência de valores/horários, resposta compartilhada, três períodos vazios, validação e autenticação. |
| `dotnet run --project tests/sessions --no-restore` | 24 verificações HTTP aprovadas, incluindo consulta da travessia autenticada, histórico e recusa anônima. Host efêmero de teste, sem SQL. |
| Checks existentes `weather`, `services`, `conecta`, `priority` | Todos aprovados. Alguns projetos emitiram NU1900 por indisponibilidade do índice de auditoria NuGet. |
| `npm.cmd run build` | TypeScript e build React aprovados. |
| `scripts/build-mobile-web.ps1` | Análise Flutter sem problemas, 18 testes aprovados, um teste de integração SQL ignorado por exigir ambiente/credenciais; build web release gerado em `mobile/build/web`. Dois testes novos cobrem ambos os sentidos, atualização manual, modo escuro e falha da API preservando acesso à fonte. |
| Análise Flutter após formatação final | Aprovada em cópia temporária para build Android. |
| `flutter build apk --debug` | Tentado com SDK Android e JBR do Android Studio. Bloqueado no download do Gradle por `SSLHandshakeException / PKIX path building failed`. Nenhum APK validado nem teste em dispositivo físico realizado. |

Para evitar escrita da telemetria Flutter fora das áreas permitidas, os comandos usaram APPDATA/LOCALAPPDATA/PUB_CACHE em diretório de trabalho e cópias temporárias com caminho ASCII, conforme os scripts existentes. O build web avisou sobre suporte Wasm de dependências existentes e fontes Cupertino; o build JavaScript terminou com sucesso. Não foi alterada a validação de certificados.

Os testes confirmam o funcionamento da alternativa assistida e a ausência de regressões nas verificações executadas; **não constituem evidência de uma consulta externa em tempo real funcionando**.
