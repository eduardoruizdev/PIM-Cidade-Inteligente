# Correções de execução: mensagens, tabelas e mapa

Os logs enviados pelo usuário confirmam API iniciada, SQL conectado e Open-Meteo HTTP 200. A tabela `AlertaMeteorologico` ainda não existe nesse banco; Overpass retornou 502 e timeout. Esses eventos são independentes do envio de mensagens.

Foi identificado um conflito de sessão: painel e Flutter usavam o mesmo cookie, e cookies não são separados por porta. Agora a equipe usa `SantosConecta.Auth` e o cidadão usa `SantosConecta.Citizen`, com esquemas autenticados distintos. O Flutter envia `X-Santos-Client: citizen`, o React envia `staff`; isso seleciona a sessão, sem conceder permissões. As permissões continuam verificadas no servidor. É necessário reconstruir o Flutter e entrar novamente nos dois aplicativos. O envio no painel confirma a gravação; confirmação de leitura continua dependendo do cidadão.

Para atualizar a instalação, no terminal da API pressione Ctrl+C e execute na raiz:

```powershell
npm.cmd run db:update
npm.cmd run api
```

O atualizador usa a mesma ordem de configurações Development/Local da API e aplica somente os scripts incrementais 04, 05, 06 e 07, verificando cinco tabelas de mensagens/alertas. Não executa seed nem apaga dados. Se o banco inicial não existir, os scripts 01–03 ainda precisam ser preparados como descrito no README. A execução SQL direta pelo agente continua bloqueada por SSPI/TLS; execute o atualizador no PowerShell do usuário, onde a API já conseguiu acesso. Não é necessário mudar autenticação ou desligar criptografia.

O servidor Overpass passou a ser consultado com GET, limite de 12 segundos por tentativa e uma segunda instância independente quando a primeira falha. Uma resposta parcial com `remark` é rejeitada. Falha de ambas aplica espera de um minuto, sem desenhar pontos inventados. A disponibilidade dos serviços públicos continua externa ao projeto. O retângulo em regiões como Orla indica somente o recorte da consulta de incidentes; não é limite oficial nem classificação de trânsito. Avenidas continuam usando os pontos reais retornados pelo OSM.

Os testes de sessões usam um host efêmero local com identidades de fixture, sem SQL ou mensagens reais. Os testes de geometria cobrem fallback entre fontes e cooldown quando ambas falham. Não se deve interpretar esses testes como validação de recebimento pelo cidadão real.

Validação desta correção: API compilada em diretório separado para preservar a instância do usuário; atualizador compilado; oito verificações HTTP de sessão aprovadas; dez verificações de clima/geometria aprovadas; React compilado; mapa testado no Chrome com fixtures (avenidas, troca, limpeza e recorte de Orla); Flutter analyze sem problemas, seis testes aprovados, um teste de SQL ignorado, Flutter web recompilado. O atualizador tentou conectar neste executor e retornou SQL 20 antes de executar os scripts. O primeiro host de teste precisou trocar a proteção de chaves do Windows por um provedor efêmero exclusivo do teste; a configuração da API de produção foi preservada. O NuGet emitiu NU1900 por falta de acesso à consulta de vulnerabilidades. Nenhuma mensagem ou notificação foi enviada para cidadãos reais nesta validação.

Referência das instâncias Overpass: [OpenStreetMap Wiki](https://wiki.openstreetmap.org/wiki/Overpass_API).
