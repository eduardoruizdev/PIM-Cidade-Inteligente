# Serviços Próximos e aparência — 03/11/2026

## Funcionalidades

Flutter: nova entrada **Serviços** na navegação inferior, abrindo **Serviços Próximos**. Pesquisa por nome, categorias, bairro, lista paginada, mapa, detalhes e botão para navegação externa. O catálogo oferece os oito filtros solicitados: UBS/policlínicas, UPA, hospitais públicos, escolas públicas, assistência social, unidades municipais, bibliotecas e esporte/cultura. A definição de categorias é centralizada em `ServicosPublicosService.Categorias`; a classificação dos títulos oficiais fica na mesma classe e pode ser estendida.

Os cards e detalhes apresentam nome, categoria, endereço, bairro, telefone, horário, distância aproximada, fonte e data de consulta. Campos ausentes são **Não informado**. Horário de cadastro e horário de atendimento não são inferidos de uma descrição genérica. Unidade sem coordenadas permanece na lista, com **Ver no mapa** desabilitado. **Como chegar** abre o Google Maps com coordenadas ou nome/endereço oficial e Santos; não inclui a localização do cidadão como origem na URL.

A localização só é solicitada ao tocar em **Usar minha localização**. Ao autorizar, os resultados são ordenados por distância em linha reta e o mapa pode centralizar na posição. Ao negar, com GPS desligado ou falha, pesquisa, lista e seleção manual de bairro continuam disponíveis. Não há persistência da posição no SQL ou no cache compartilhado. A permissão e as capacidades do aplicativo externo continuam dependentes do sistema operacional.

O mapa usa o FlutterMap existente. Marcadores têm ícones e cores por categoria, tooltip com nome e acesso aos detalhes. Agrupamento por células depende do zoom: grupos aproximam o mapa e, em zoom elevado, abrem uma lista para escolher unidades sobrepostas. Movimentos do mapa recalculam apenas os marcadores locais, sem chamadas externas. Listas com mais de cem registros usam **Carregar mais**.

## Fonte oficial e qualidade

Fonte escolhida após inspeção: [Catálogo de locais da Prefeitura de Santos](https://www.santos.sp.gov.br/locais). A página publica uma coleção JSON de locais com `nid`, `title`, `field_endereco`, `field_telefone` e `field_ponto`. Foi preferida à complementação por estabelecimentos OSM sem identificação clara de gestão pública. O OpenStreetMap é utilizado como mapa de base, não como cadastro substituto.

O adaptador lê a coleção publicada e inclui somente títulos reconhecidos como unidades públicas nas categorias suportadas. Hospitais sem identificação municipal/estadual ou nome público reconhecido não são classificados como públicos por suposição. O catálogo municipal pode ser incompleto ou conter informações antigas. A classificação conservadora também pode deixar de incluir unidades cujo título não identifique sua natureza. Filtro sem resultados não significa inexistência do serviço na cidade.

`bairro` na coleção contém IDs e não foi apresentado como nome de bairro. Quando possível, o nome é extraído do trecho explícito do endereço após um separador; se não houver um nome utilizável, permanece ausente. Isso é uma limitação do filtro: ele não cobre todos os bairros/unidades. Não foram inventadas coordenadas nem executado geocoding aproximado para completar registros.

A coleção não fornece uma data confiável de atualização por unidade nem horários padronizados completos. `consultadoEm` é a data em que o backend baixou o catálogo; **não é a confirmação de atualização da unidade**. `atualizacaoFonte` fica nula quando não informada. Confirme horários e atendimento diretamente na unidade.

Cache em memória de 24 horas. Uma cópia obtida anteriormente pode ser usada por até sete dias quando a fonte falha, identificada como desatualizada, preservando a data original. Falhas aplicam um intervalo de um minuto antes de nova tentativa. Não existe fallback para dados fictícios. Reiniciar a API remove esse cache. Uma alteração no formato da página gera indisponibilidade explícita, não uma lista vazia apresentada como completa.

## Endpoints .NET

Todos preservam a autenticação existente da API. O cliente cidadão usa seu cookie e o cabeçalho `X-Santos-Client: citizen`.

| Método | Endpoint | Comportamento |
|---|---|---|
| GET | `/api/servicos-publicos` | Lista, total, bairros disponíveis, fonte, data e indicador de desatualização |
| GET | `/api/servicos-publicos/{id}` | Detalhes; ID oficial normalizado como `santos-<nid>`; 404 se ausente |
| GET | `/api/servicos-publicos/categorias` | IDs e nomes dos filtros disponíveis |

Parâmetros: `categoria`, `bairro`, `texto`, `latitude`, `longitude`, `raio` em km, `limite` (1–200; padrão 100) e `inicio` (0–5000). Coordenadas devem vir em par, dentro dos limites geográficos e sem NaN/infinito. Raio exige coordenadas, deve ser positivo e de até 100 km. Filtros inválidos retornam 400. A distância usa Haversine e não é distância de trajeto nem tempo de viagem. Um filtro por raio exclui registros sem coordenadas conhecidas.

Nenhuma tabela, migration, seed ou alteração destrutiva no SQL foi necessária. Não há chave de API nova.

## Aparência

Flutter: `ThemeData`, `darkTheme` e `ThemeMode` com **Claro**, **Escuro** e **Automático (sistema)**. Preferência salva localmente pelo `flutter_secure_storage`, dependência já existente, em chave separada da sessão. É carregada antes da abertura do aplicativo. Não é apagada ao sair da conta. Em automático, o MaterialApp acompanha as mudanças de brilho do sistema.

O seletor está em **Perfil → Aparência**, no botão de aparência da página inicial e nas telas de entrada/cadastro. Foram trocadas cores fixas por papéis de `ColorScheme` nas telas e widgets de identidade, reclamações, mensagens, trânsito, clima, alertas e Conecta. Cores semânticas do trânsito e dos marcadores permanecem identificáveis. A base OSM clara foi preservada: não foram aplicados filtros que distorçam cores de tráfego ou alertas.

React: seletor **Aparência** no cabeçalho do portal `/cidadao`, inclusive no login. A preferência usa a chave `santos-citizen-appearance` no armazenamento local; o modo automático acompanha `prefers-color-scheme` em tempo real. CSS é limitado a `.citizen-portal[data-theme=dark]`; não se aplica um tema novo às páginas administrativas. O Conecta e os componentes reutilizados no portal acompanham o tema. Preferências são por aplicativo/navegador, sem sincronização entre dispositivos.

Foi adicionada somente a dependência Flutter `url_launcher`, necessária para abrir o aplicativo de navegação. O agrupamento usa código local e não acrescenta biblioteca de clustering. Semântica e tooltips existentes foram preservados.

## Validação

- API .NET compilada sem erros; compilação de verificação em diretório separado para não substituir processos do usuário.
- 12 verificações de catálogo com fixtures identificadas: exclusão de hospital privado, campos ausentes, coordenadas inválidas, filtros por nome/categoria/bairro, cache, distância/raio, isolamento dos dados de localização, detalhes, validação, mudança de layout, falha/cooldown e cópia desatualizada.
- 21 verificações HTTP com host de teste e fontes de fixture: incluem categorias, lista filtrada, detalhes, 404, 400 e autenticação, além das verificações anteriores de sessões e Conecta. Não conectam ao SQL nem alteram dados reais.
- Flutter analyze sem problemas; 16 testes aprovados, incluindo os testes anteriores e seis novos: marcador abre detalhes sem nova consulta, persistência dos três temas em nova instância do controlador, mudança automática do sistema, URL de destino sem origem GPS e localização autorizada/negada com pesquisa e acionamento da rota. Um teste de SQL real foi ignorado por exigir fixture e credenciais.
- A autorização/negação de GPS e a abertura de rota foram validadas com implementações de teste injetadas. Não foi alegado teste em telefone físico ou abertura efetiva de Google Maps instalado.
- A preferência Flutter foi restaurada em uma nova instância, simulando reinicialização do controlador. O React foi testado com recarga real de página e mudanças de esquema do navegador.
- React/TypeScript/Vite compilados. Chrome com fixtures validou claro/escuro/automático, persistência após reload, reação ao sistema e os fluxos existentes do Conecta para protocolo, clima, trânsito e formulário, além de manter o chat fora do login administrativo. Capturas de fixture existentes foram atualizadas em `artifacts/conecta-fixture-mobile.png` e `conecta-fixture-desktop.png`.
- Flutter web compilado pelo script existente; os avisos conhecidos sobre Wasm do secure_storage e fonte Cupertino não impedem a compilação JavaScript.
- A fonte oficial foi inspecionada pela pesquisa web. A tentativa de consulta real pelo processo .NET deste executor falhou; portanto o download e a interpretação do catálogo real pela API **não foram comprovados neste ambiente**. O teste `--live` é reproduzível no PowerShell do usuário, onde outras integrações HTTPS já funcionaram.

Os dados de fixtures existem somente nos testes. Não foram incorporados ao catálogo de produção. Não foram realizados testes com SQL real, APK ou dispositivo físico nesta entrega. O NuGet pode apresentar NU1900 quando não consegue consultar vulnerabilidades; a auditoria externa não foi confirmada.

Comandos:

```powershell
dotnet run --project tests/services
# Consulta pública real, somente leitura, no ambiente com HTTPS disponível:
dotnet run --project tests/services -- --live
dotnet run --project tests/sessions
npm.cmd run build
npm.cmd run test:mobile
npm.cmd run mobile:web:build
# Preview React na porta 5177 para teste de interface:
node scripts/test-conecta-ui.mjs
```

## Arquivos da entrega

Criados: `backend/SantosConecta.Infrastructure/Services/ServicosPublicosService.cs`, `backend/SantosConecta.API/Controllers/ServicosPublicosController.cs`, `mobile/lib/services/appearance.dart`, `mobile/lib/screens/nearby_services_screen.dart`, `mobile/test/services_theme_test.dart`, `tests/services/ServiceChecks.csproj`, `tests/services/Program.cs` e este relatório.

Modificados:

- `backend/SantosConecta.API/Program.cs` (cliente HTTP do catálogo).
- `mobile/lib/main.dart` (preferência inicial e temas globais).
- `mobile/lib/screens/home_screen.dart`, `auth_screens.dart`, `new_occurrence_screen.dart`, `occurrence_detail_screen.dart`, `traffic_screen.dart`, `weather_screen.dart`, `weather_alerts_screen.dart`, `conecta_screen.dart` (cores do tema, navegação e aparência).
- `mobile/lib/widgets/identity.dart`, `notification_button.dart`, `occurrence_messages.dart`, `weather_alert_banner.dart` (cores e contraste).
- `mobile/lib/services/api_client.dart` (formatação pelo Dart, sem alteração do protocolo de autenticação).
- `mobile/pubspec.yaml` e `pubspec.lock`.
- `src/pages/CitizenPortal.tsx`, `src/styles-conecta.css`, `scripts/test-conecta-ui.mjs` (tema do cidadão e testes).
- `tests/sessions/Program.cs`, `README.md` e `docs/FLUTTER.md`.

## Para usar

Reinicie a API no seu terminal com `npm.cmd run api`. Atualize o Flutter web com Ctrl+F5 no servidor já aberto na porta 5200 e procure **Serviços**. Para aparência, use o botão no topo ou **Perfil → Aparência**. No React, acesse `/cidadao` e escolha o tema no cabeçalho. Não é necessário aplicar SQL para estas duas funcionalidades.

Configurações pendentes: acesso HTTPS do backend ao portal municipal e permissão de localização quando desejada pelo cidadão. APK e navegação externa em telefone devem ser validados no ambiente Android já configurado. O catálogo não garante cobertura integral dos equipamentos públicos de Santos.
