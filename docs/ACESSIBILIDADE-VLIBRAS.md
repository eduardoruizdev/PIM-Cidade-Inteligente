# Acessibilidade com VLibras

## Portal React

O portal inclui o widget oficial do Governo Federal em todas as rotas React, inclusive login e portal do cidadão. O script `https://vlibras.gov.br/app/vlibras-plugin.js` é carregado sob demanda uma única vez; falhas do serviço externo não impedem o uso da plataforma. O botão oficial oferece nome acessível e foco de teclado, e o widget é mantido fora da árvore de rotas para sobreviver à navegação.

O botão fica à direita, na posição padrão do VLibras. A ferramenta oficial permite escolher direita/esquerda, mas não fornece configuração documentada para canto inferior; o posicionamento vertical fica sob controle do widget para preservar seus controles e acessibilidade.

O projeto não define Content Security Policy (CSP) própria atualmente. Se a hospedagem adicionar CSP, permita o script `https://vlibras.gov.br`, `frame-src https://vlibras.gov.br` e `font-src https://vlibras.gov.br https://cdn.jsdelivr.net`, conforme a documentação oficial. Valide também os demais recursos externos observados no console do navegador, sem abrir permissões mais amplas que o necessário.

## Aplicativo Flutter

O widget oficial é distribuído como JavaScript para páginas web e seu avatar usa um iframe/web assets. O app Santos Conecta usa telas Flutter nativas em Android/iOS, não uma página web. Não há integração SDK nativa Flutter publicada na documentação consultada; incorporar o widget em WebView não disponibilizaria de modo confiável a tradução de texto das telas nativas e exigiria substituir a interface por conteúdo web. Por isso o widget não foi simulado nem inserido no app nativo.

Alternativas atuais: usar o portal web responsivo do Santos Conecta com o widget, ou instalar o aplicativo oficial VLibras no smartphone e usá-lo como recurso independente. A página do Governo Federal mantém links para as versões disponíveis para Android e iOS. A integração do app nativo deve aguardar um SDK oficial compatível ou uma solução de acessibilidade validada em aparelhos reais.

## Referências oficiais

- [Integração em páginas web e parâmetros do widget](https://vlibras.gov.br/doc/widget/installation/webpageintegration.html)
- [Requisitos de Content Security Policy](https://vlibras.gov.br/doc/widget/installation/content-security-policy.html)
- [Suíte VLibras e aplicativos oficiais](https://www.gov.br/governodigital/pt-br/acessibilidade-e-usuario/vlibras)
