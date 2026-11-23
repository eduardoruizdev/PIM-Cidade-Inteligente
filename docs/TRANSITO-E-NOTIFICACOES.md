# Interface, notificações e trânsito — 02/11/2026

No ambiente atualmente inspecionado, `TOMTOM_API_KEY` nao esta configurada. A consulta sem chave retornou HTTP 401; portanto, nao foi possivel validar fluxo ao vivo nesta execucao. O backend retorna indisponivel e as interfaces nao afirmam condicoes atuais ate que uma chave valida seja configurada. `Traffic:Enabled` esta habilitado por padrao no appsettings, mas a chave continua obrigatoria.

## Fluxo TomTom cobrindo o mapa

Além da consulta de segmento próximo a uma referência, React e Flutter podem exibir a camada de Raster Flow Tiles `relative0` da TomTom em todas as vias com dados nos tiles visíveis. O backend a serve em `/api/transito/tiles/flow/{z}/{x}/{y}`; a chave permanece no servidor, a rota requer sessão autenticada e as respostas não são armazenadas. O limite local padrão é 10.000 tiles por mês (`Traffic:MaxTileRequestsPerMonth`), separado das consultas de segmento/incidentes. Esse contador em memória reinicia quando a API reinicia e serve como proteção local, não como controle de faturamento. Configure-o conforme o plano e o consumo esperado; a cota e cobrança da conta TomTom prevalecem. A cobertura depende das vias e dos dados fornecidos pela TomTom para cada tile e não garante que toda rua de Santos terá uma condição.

## Executar os dois clientes

Na raiz do projeto, mantenha três terminais abertos:

```powershell
# Terminal 1: API compartilhada, porta 5080
npm.cmd run api

# Terminal 2: painel
npm.cmd run dev

# Terminal 3: aplicativo no navegador
# Recompile depois de mudar os arquivos Dart.
npm.cmd run mobile:web:build
npm.cmd run mobile:web
```

Painel: http://localhost:5173. Aplicativo: http://127.0.0.1:5200. Use esses hosts diferentes para separar as sessões de equipe e cidadão no mesmo navegador. O proxy do aplicativo agora usa 5080 por padrão; `MOBILE_API_URL` permite alterar o destino. `127.0.0.1` deve ser aberto no computador que executa os servidores; acesso de um telefone físico depende da configuração da rede descrita em FLUTTER.md.

## Configuração TomTom

O nome escolhido pelo usuário foi preservado: **backend/SantosConecta.API/appsettings.Local.example.json**. Apesar do nome, esse arquivo agora contém configuração privada local e não é um modelo versionado. Ele foi retirado do índice do Git sem apagar a cópia no disco e está no .gitignore.

Em Development, a API carrega `appsettings.Local.json`, depois `appsettings.Local.example.json`. Variáveis de ambiente têm precedência. Nenhum desses arquivos locais é carregado em produção.

Propriedades que devem existir no objeto principal do JSON, junto das configurações do banco:

```json
{
  "TOMTOM_API_KEY": "PREENCHER_SOMENTE_NO_ARQUIVO_LOCAL",
  "Traffic": {
    "Enabled": true,
    "IncidentsEnabled": true,
    "MaxRequestsPerMonth": 2000,
    "MaxTileRequestsPerMonth": 10000
  }
}
```

Alternativa no terminal da API:

```powershell
$env:TOMTOM_API_KEY = 'CHAVE_LOCAL'
$env:Traffic__Enabled = 'true'
$env:Traffic__IncidentsEnabled = 'true'
$env:Traffic__MaxTileRequestsPerMonth = '10000'
```

Reinicie a API depois de alterar a configuração. A chave nunca é entregue ao React ou ao Flutter; os clientes consultam `/api/transito?regiao=Orla`. Os loggers HTTP desse adaptador são removidos para não registrar URLs autenticadas.

## Escolha do provedor e alcance

TomTom foi mantido porque sua API REST pode ser normalizada no backend e consumida pelo Leaflet e pelo flutter_map existentes. Google TrafficLayer exige o mapa Google no cliente; sua utilização depende das condições de faturamento da plataforma. Não houve contratação, criação de conta ou alteração de plano.

A tabela oficial TomTom informa cobertura de fluxo e incidentes no Brasil. A disponibilidade em cada via depende dos dados retornados pelo provedor. Flow Segment Data retorna o fragmento viario mais proximo da referencia consultada. As opcoes do filtro sao referencias dentro de Santos; o sistema nao apresenta as outras cidades da regiao como areas monitoradas.

Incident Details v5 retorna incidentes atuais no recorte da região. A consulta usa `timeValidityFilter=present` e `language=pt-PT`: o serviço rejeita pt-BR. Os recortes são retângulos de consulta, não polígonos municipais oficiais, e podem alcançar vias vizinhas. Lista vazia significa nenhum incidente informado naquele recorte, não garantia de ausência de problemas.

As interfaces consultam a cada 60 segundos enquanto ativas, mostram o horario UTC convertido para apresentacao local e retiram a camada apos 120 segundos sem informacao valida ou em caso de erro. O horario e o da consulta ao provedor, nao o instante comprovado da medicao de cada veiculo. Os indicadores resumem somente os trechos devolvidos pela API; nao estimam o total de vias da cidade.

O backend não armazena respostas de trânsito nem serve cópias antigas após falhas. O endpoint é `no-store`. Há proteção de dez segundos entre consultas à mesma região e limite conservador de requisições por processo/mês. Esse contador é em memória, reinicia com o processo e não substitui quotas ou limites financeiros na conta TomTom. As atribuições OpenStreetMap e TomTom permanecem nas telas.

Na pagina oficial consultada em 22/11/2026, Flow Segment Data apresenta franquia gratuita de 20 mil requisicoes mensais, Incident Details 2,5 mil e Raster Flow Tiles 200 mil. A franquia e por API e por conta, sujeita aos termos e alteracoes do provedor. O contador local do backend nao representa a cota da conta.

Fontes oficiais consultadas:

- [Cobertura TomTom](https://docs.tomtom.com/traffic-api/documentation/tomtom-maps/v1/product-information/market-coverage)
- [Fluxo por trecho](https://docs.tomtom.com/traffic-api/documentation/tomtom-maps/v1/traffic-flow/flow-segment-data)
- [Incidentes, idiomas e filtro temporal](https://docs.tomtom.com/traffic-api/documentation/tomtom-maps/v1/traffic-incidents/incident-details)
- [Preços e franquias](https://docs.tomtom.com/pricing)
- [Google TrafficLayer](https://developers.google.com/maps/documentation/javascript/trafficlayer)
- [Faturamento Google Maps](https://developers.google.com/maps/documentation/javascript/usage-and-billing)

## Modal de funcionários

Cadastro e edição usam o mesmo dialog nativo com fundo bloqueado, título e campos rotulados, foco contido no modal e retorno do foco ao botão original. ESC, Cancelar e X fecham quando o formulário não está salvando. Durante uma operação, os campos e botões de fechamento ficam desabilitados. A altura respeita a janela e o conteúdo tem rolagem. Campos em duas colunas no desktop e uma no celular. Alterar a situação de conta existente exige confirmação em um segundo modal pequeno. Não há exclusão física.

Edição sem nova senha preserva o hash existente. Somente Administrador acessa a gestão, tanto nas rotas da API quanto na interface. A desativação invalida sessões da conta. O administrador não pode retirar o próprio acesso administrativo.

## Notificações persistidas

As mensagens de `MensagemOcorrencia` são a origem das notificações. Não há cópia do conteúdo em outra tabela ou criação de múltiplas notificações para a mesma mensagem. Assim que a mensagem é salva, ela passa a integrar a consulta correspondente.

- Cidadão: recebe somente mensagens da equipe de ocorrências de sua autoria. Leitura no campo `MensagemOcorrencia.DataLeitura` existente.
- Funcionário: recebe respostas de cidadãos em ocorrências atribuídas a ele e em ocorrências sem responsável, que formam a caixa compartilhada.
- Administrador: acompanha respostas em todas as ocorrências autorizadas.
- Equipe: leitura individual em `LeituraMensagem`, com chave composta mensagem/funcionário. Uma leitura não apaga avisos de outros funcionários ou administradores. A atribuição atual da ocorrência determina a caixa de entrada; um novo responsável pode ver o histórico correspondente.

Aplique `database/06-notificacoes.sql` depois dos scripts 01–05. O script é repetível, tem FKs e índices e não apaga dados. Foi aplicado no SQL Server local. Não são usadas migrations EF concorrentes.

Endpoints autenticados:

- `GET /api/notificacoes`: até 100 mensagens, não lidas primeiro, com protocolo, categoria, resumo, remetente e datas.
- `GET /api/notificacoes/nao-lidas/quantidade`: contagem completa, independente do limite da lista.
- `PUT /api/notificacoes/{idMensagem}/lida`: leitura idempotente, autorizada com a identidade do cookie.
- `GET/POST /api/ocorrencias/{id}/mensagens`: conversa privada existente.
- `POST /api/ocorrencias/{id}/mensagens/lidas`: marca mensagens recebidas elegíveis da ocorrência para o usuário atual.

O sino consulta lista e contador a cada 30 segundos. A conversa aberta consulta a cada 15 segundos. React pausa consultas de intervalo em segundo plano; Flutter observa o ciclo de vida e a conversa também pausa quando sua rota está coberta. O sino Flutter mantém a central atualizada durante sua exibição. Abrir o aviso leva diretamente à conversa, destaca a mensagem e persiste sua leitura. Avisos lidos permanecem no histórico. Navegar apenas pela lista de notificações não marca outras mensagens como lidas.

Não há infraestrutura de push configurada: as notificações são internas ao aplicativo, atualizadas ao abrir e durante o uso. Nenhum aviso com app fechado foi implementado ou validado.

## Verificação reproduzível

```powershell
npm.cmd run build
dotnet build SantosConecta.sln --no-restore
$env:API_TEST_URL = 'http://127.0.0.1:5080'
$env:WEB_TEST_URL = 'http://127.0.0.1:5173'
node scripts/test-admin-web-v2.mjs
node scripts/test-v2.mjs --flutter
$env:EXPECT_LIVE_TRAFFIC = 'true'
node scripts/test-mobile-web-v2.mjs
node scripts/test-traffic-live.mjs
```

Os scripts de integração criam dados fictícios novos, usando senhas aleatórias sem imprimi-las. Não alteram credenciais de contas anteriores. O teste de trânsito real faz chamadas ao provedor configurado. Resultados e limites efetivamente observados estão em VALIDACAO.md; um script existente não significa que foi executado.
