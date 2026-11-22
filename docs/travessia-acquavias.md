# Travessia Santos–Guarujá: integração AcquaVias

## Fonte e leitura

A fonte é a página oficial da [AcquaVias para Santos–Guarujá](https://acquavias.com.br/santos-guaruja/). Em 22/11/2026, o HTML servido pela página continha os tempos dos dois lados no bloco `data-travessia-id="1951"`, a quantidade no atributo acessível `aqv-topbar__embarcacoes` e avisos operacionais na classe `aqv-aviso-usuario`. A página usa esses mesmos blocos para mostrar os tempos na interface.

Na captura HTTP usada para validar o parser, o HTML informava 12 minutos nos dois lados, 8 embarcações e lentidão por maré forte. Esses são valores daquele instante, não constantes da aplicação.

O HTML também contém JavaScript que faz `GET https://acquavias.com.br/wp-admin/admin-ajax.php?action=aqv_topbar_atualizar` a cada minuto e substitui o conteúdo da barra com o campo `data.html` de uma resposta JSON. Uma chamada direta reproduzindo a requisição retornou HTTP 400 com corpo `0` neste ambiente. Por isso o backend lê o HTML público, que já contém o dado renderizado, sem contornar o bloqueio nem depender de CORS no Flutter. O serviço valida a identificação da travessia e os dois valores antes de aceitar a leitura.

O site não publica no bloco consultado o horário original em que a espera foi medida. `capturadoEm` e `consultadoEm` significam somente o horário em que o backend leu a página; o app mostra esse rótulo e não o apresenta como hora da medição.

## Backend

- `GET /api/travessias/santos-guaruja` consulta o cache mantido pelo serviço e preserva a autenticação já existente.
- `GET /api/travessias/santos-guaruja/historico?periodo=horas|hoje|7dias` segue disponível, mas informa histórico vazio até haver armazenamento de leituras.
- `TravessiaWorker` faz uma leitura na inicialização e tenta atualizar a cada minuto. Timeout, resposta HTTP inválida ou estrutura inesperada não apagam a última leitura válida; depois de três minutos ela é marcada como desatualizada.
- Não são inferidos avisos, situação operacional ou horários ausentes. O contador de embarcações e os avisos só aparecem se forem encontrados na resposta oficial.

## Limitações

A AcquaVias pode alterar o HTML ou a identificação `1951`; nesse caso, o parser rejeita o conteúdo e a API informa indisponibilidade ou mantém a última leitura válida marcada como antiga. A atualização se refere à leitura do HTML, não a um SLA nem à frequência real de aferição pela operadora. O endpoint da aplicação continua exigindo autenticação conforme a política atual.

O Flutter Web usa a mesma origem da API por padrão, mantendo os cookies de sessão. Em Android, o padrão é `10.0.2.2:5080` para emulador; aparelho físico e produção devem definir `API_URL` via `--dart-define` para o endereço alcançável da API. HTTPS é necessário fora do desenvolvimento local.

Validação: `dotnet run --project tests/ferry --no-restore -p:OutputPath=artifacts/ferry-tests/`. A consulta live deste ambiente não conseguiu completar o TLS via `HttpClient`; foi confirmada separadamente a disponibilidade do HTML com Node usando a cadeia de certificados do sistema. A integração não relaxa validação TLS.
