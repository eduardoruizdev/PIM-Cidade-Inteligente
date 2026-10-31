# Organização do trabalho — Scrum adaptado

**Produto:** Santos Conecta, da empresa fictícia Conecta Soluções Urbanas.
**Equipe:** um desenvolvedor principal; o grupo acadêmico revisa as entregas e representa o Product Owner.
**Duração planejada:** duas semanas, com uma sprint de cinco dias úteis por semana.

Este é um plano acadêmico de execução. Não representa datas ou cerimônias que tenham ocorrido de fato.

## Backlog priorizado

| ID | História de usuário | Prioridade | Aceite |
|---|---|---|---|
| US01 | Como funcionário, quero entrar com minhas credenciais para acessar o atendimento. | Alta | Senha com hash, login válido, cookie protegido e 401 sem autenticação. |
| US02 | Como funcionário, quero cadastrar e editar uma reclamação para registrar o problema. | Alta | API valida campos, grava cidadão/categoria/responsável e retorna protocolo único. |
| US03 | Como funcionário, quero alterar status e responsável para acompanhar o atendimento. | Alta | Alteração persistida e histórico com autor; atualização nas telas. |
| US04 | Como gestor, quero consultar indicadores para acompanhar as reclamações. | Alta | Totais do banco e filtros consistentes por status/prioridade. |
| US05 | Como funcionário, quero localizar reclamações no mapa para avaliar sua distribuição. | Alta | Coordenadas do banco, marcadores, agrupamento e calor. |
| US06 | Como funcionário, quero responder por e-mail para informar o cidadão. | Alta | Revisão antes do envio; histórico distingue simulação, sucesso SMTP e falha. |
| US07 | Como funcionário, quero gerar uma sugestão administrativa para redigir a resposta. | Média | Groq no backend; modo simulado identificado; envio sempre manual. |
| US08 | Como integrante do grupo, quero executar e explicar a aplicação para apresentá-la. | Alta | README, scripts SQL, Swagger e registro honesto das verificações. |
| US09 | Como funcionário no celular, quero navegar e preencher formulários. | Média | Menu recolhível, tabelas com rolagem e formulários sem transbordamento. |

## Sprint 1 — Base e cadastro

Objetivo: organizar a interface e concluir o caminho React → Controller → Service → Repository → SQL Server.

- Dias 1–2: revisão visual, retirada de Relatórios e substituição da comunicação.
- Dia 3: tabelas, relacionamentos, três procedures e seed fictício.
- Dias 4–5: quatro projetos, entidades, DbContext, autenticação e CRUD principal.
- Revisão: demonstrar cadastro, consulta e alteração de uma reclamação.

## Sprint 2 — Integração e entrega

Objetivo: integrar acompanhamento, comunicação e documentação.

- Dias 6–7: serviços HTTP, atualização do cache, indicadores e mapa com dados do banco.
- Dia 8: SMTP, histórico de e-mails e Groq com revisão humana.
- Dia 9: testes de API, fluxo no navegador e responsividade.
- Dia 10: correções, README, roteiro de apresentação e retrospectiva.

## Acompanhamento e definição de pronto

Usar quadro **A fazer → Em andamento → Em revisão → Concluído**. Limitar o trabalho em andamento a uma história principal. Registrar diariamente o avanço e os impedimentos em poucas linhas. A revisão de cada sprint verifica o objetivo; a retrospectiva identifica uma melhoria para a etapa seguinte.

Uma história está pronta quando compila, possui validação adequada, foi verificada no ambiente aplicável, tem comportamento de erro visível e está documentada. Uma dependência sem credencial ou sem acesso ao banco permanece explicitamente pendente; código escrito não equivale a integração validada.
