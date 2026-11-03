# Revisão da travessia — 03/11/2026

## Resultado

Redesign Flutter e correções de interpretação da API e consulta externa implementados. **Tempos reais continuam indisponíveis**: não foi confirmada uma fonte com acesso automatizado permitido, dados recentes e horário original verificável. Nenhuma espera foi simulada ou inserida no banco.

## Causa identificada

O serviço anterior retornava sempre uma constante de indisponibilidade: não existiam HttpClient, parser, cache de medições ou tentativa de coleta. A tela ignorava `sentidos` da resposta e apresentava textos fixos. Portanto, a ausência de tempos não era uma falha de parsing ou de comunicação Flutter–API. Os testes HTTP autenticados confirmam a comunicação.

O link anterior era uma constante no Flutter e aparecia inclusive com a API fora do ar. A ausência de verificação de acessibilidade fazia o aplicativo oferecer um endereço sem conseguir confirmar que estava funcionando.

## Pesquisa e consultas reais

Comando reproduzível: `node --use-system-ca scripts/check-ferry-sources.mjs`. Resultado detalhado em `artifacts/ferry-sources.json`; somente metadados, sem cópia de conteúdo de terceiros.

| Fonte | Evidência desta revisão | Decisão |
| --- | --- | --- |
| [SEMIL, Santos–Guarujá](https://semil.sp.gov.br/travessias/travessias-automoveis/santos-guaruja/) | HTTP 200, HTML UTF-8, cerca de 12,6 segundos; em tentativa anterior excedeu 20 segundos. A própria página informa que as informações estão desatualizadas. | Referência oficial mantida. Não usar seus números como medições atuais. |
| [METROA](https://metroa.com.br/santos/balsas/santos-guaruja) | HTTP 530, título indicando erro de túnel Cloudflare. A pesquisa web apresenta conteúdo indexado, mas isso não comprova disponibilidade HTTP atual. | Alternativa externa somente se posteriormente validada. Nenhuma coleta ou contorno do bloqueio. |
| [Índice oficial de travessias](https://semil.sp.gov.br/travessias/) | HTTP 200 em cerca de 12,3 segundos. | Não fornece, por si só, um contrato público de dados atuais. |
| `https://www.dh.sp.gov.br/travessias/` | Falha de DNS `ENOTFOUND`. | Não adotado. |
| [ISN Online](https://isnonline.com.br/balsas-ao-vivo/) | HTTP 200, página HTML, cerca de 2,5 segundos. | Publicação de terceiro; não foi identificado feed documentado com permissão de reutilização e timestamp original verificável. Não adotado como API. |

Na pesquisa de documentação não foi encontrada API pública documentada para tempos da balsa. Isso não prova inexistência. `robots.txt` da METROA respondeu 200 e inclui sinais de conteúdo e restrições específicas a agentes; a regra genérica `Allow: /` não licencia a republicação. O arquivo da SEMIL respondeu 200 e tem restrições por agente. Não foram interpretados como autorização para extrair e republicar medições.

O Node sem a cadeia de confiança do sistema falhou com `UNABLE_TO_VERIFY_LEAF_SIGNATURE`; `--use-system-ca` permitiu os testes acima sem desativar TLS. O teste real **C# HttpClient**, com o mesmo User-Agent da aplicação, continuou falhando com `HttpRequestException / SecureConnectionError` para as duas fontes. Os logs registram host e categoria de falha. Não foi alterada a validação de certificados do backend.

## Interface e integração

- Cabeçalho editorial, identidade verde do tema global, esquema acessível da travessia e cards com identificação por sentido, hierarquia de espera e estado de confiabilidade.
- Layout em coluna no telefone e duas colunas quando houver largura; sem cores fixas que prejudiquem o modo escuro.
- Campos dos dois sentidos agora são interpretados da API. Valores ausentes, negativos, fracionários e fora de 0–1440 minutos não são apresentados como esperas válidas.
- Atualidade depende de disponibilidade, flag do backend e data original: até 15 minutos, com tolerância de dois minutos a relógios futuros. Consulta interna nunca é usada como data da medição. Horários exibidos em São Paulo, UTC−3.
- Medições antigas são identificadas como históricas. Em erro da API, o último estado em memória é preservado e perde o indicador de atualização confirmada.
- Carregamento, erro, nova tentativa, fonte, embarcações e comunicados condicionais, ausência explícita de histórico, atualização manual e a cada cinco minutos em primeiro plano. Timer encerrado ao sair; atualização de validade visual a cada minuto.
- Link fixo removido. `consultaExterna` da API informa URL, nome, disponibilidade e horário de verificação. O Flutter somente oferece destinos HTTPS conhecidos, com verificação recente. Abertura em navegador externo, com nova aba na web e mensagem quando a abertura falha.

## Backend

Os endpoints autenticados permanecem:

- `GET /api/travessias/santos-guaruja`
- `GET /api/travessias/santos-guaruja/historico?periodo=horas|hoje|7dias`

O primeiro agora verifica acessibilidade de páginas via HttpClient: até 15 segundos por candidato, no máximo dois candidatos, resposta HTML de sucesso com identificação da travessia e sem desafio de acesso. Lê no máximo 32.768 caracteres. Redirecionamentos manuais limitados a três, sempre HTTPS no mesmo host. O cliente de produção tem redirecionamento automático desabilitado. Não há proxy de conteúdo nem extração de tempos.

Resultados positivos **e negativos** são compartilhados em memória durante 30 minutos; semáforo impede verificações simultâneas. A tentativa manual de um cidadão não força nova consulta externa durante esse período. Links expirados não são reutilizados quando a verificação seguinte falha. Em ambiente com os erros TLS observados, a API devolve URL nula e o botão é omitido, conforme solicitado.

Não há fonte de medições escolhida para produção; SEMIL é a referência oficial. Gráficos, SQL e alertas dependem de uma futura integração validada. Não existe última medição real a recuperar; nenhum horário foi preenchido para dar aparência de atualização.

## Arquivos desta revisão

- `mobile/lib/screens/ferry_screen.dart`
- `mobile/lib/models/ferry_status.dart` (novo)
- `mobile/test/ferry_test.dart`
- `backend/SantosConecta.Infrastructure/Services/TravessiaService.cs`
- `backend/SantosConecta.API/Controllers/TravessiasController.cs`
- `backend/SantosConecta.API/Program.cs` (registro do cliente e singleton)
- `tests/ferry/Program.cs`
- `scripts/check-ferry-sources.mjs` (novo)
- `scripts/test-ferry-ui.mjs` (novo)
- Este relatório e referência no README.

## Validação

- Build da solução C#: aprovado, zero avisos/erros.
- `dotnet run --project tests/ferry --no-restore -- --live`: 17 verificações determinísticas e uma verificação de ausência de números inventados na consulta real, aprovadas. O teste real registrou a falha TLS e URL nula; não é sucesso de integração em tempo real.
- Checks existentes `sessions`, `weather`, `services`, `conecta`, `priority`: aprovados, incluindo 24 verificações HTTP e os dois endpoints de travessia. O host HTTP de testes usa o controller real, sem SQL e sem chamadas externas. Alguns projetos emitiram NU1900 pela indisponibilidade da auditoria NuGet.
- Flutter analyze: sem problemas.
- Flutter tests: 22 aprovados; um teste de SQL real ignorado por exigir fixture/credenciais. Cobertura nova inclui campos da API, ausência de informações, link validado, falha de abertura, domínio rejeitado, data antiga, modo escuro, atualização manual e tela de 320 px com fonte ampliada em 1,8×. Um transbordamento detectado foi corrigido e os testes repetidos.
- Flutter web release: build aprovado e atualizado em `mobile/build/web`. Permanecem avisos de dependências existentes sobre Wasm e fonte Cupertino; o build JavaScript terminou com sucesso.
- `node scripts/test-ferry-ui.mjs`: aprovado em Chrome headless nos modos claro e escuro, navegando pelo acesso existente do aplicativo e verificando ausência de botão para URL não validada. Perfil fixture e resposta sem medições interceptados exclusivamente no navegador de teste; nenhum cidadão ou registro SQL criado. Capturas inspecionadas em `artifacts/ferry-light-header.png`, `artifacts/ferry-light-directions.png`, `artifacts/ferry-dark-header.png` e `artifacts/ferry-dark-directions.png`.

Não houve teste em Android físico, nova validação de SQL real nem confirmação de carregamento final do site externo em dispositivo. Testes de abertura com URI e falha usam launcher injetado; isso não comprova disponibilidade da página. O diagnóstico HTTP separado acima cobre essa limitação parcialmente.

Este relatório substitui as conclusões técnicas da entrega inicial em `travessia-santos-guaruja.md`, que permanece como registro daquela etapa.
