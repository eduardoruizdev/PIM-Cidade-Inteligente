# Aplicativo Flutter — Santos Conecta

**Serviços Próximos:** disponível em **Serviços** na navegação inferior. **Aparência:** no botão do topo e em Perfil, com claro, escuro e automático. [Fonte, testes e limitações](SERVICOS-E-APARENCIA.md).

O assistente **Conecta** está na navegação inferior do cidadão. Veja [configuração e validação do assistente](CONECTA.md). O portal React correspondente está em `/cidadao`.

Material 3 e a mesma Web API C#. Firebase Cloud Messaging é opcional para avisos meteorológicos; as reclamações continuam na API .NET e no SQL Server. Veja [clima e alertas](CLIMA-ALERTAS-E-GEOMETRIA.md).

## Testar no navegador sem Android Studio

Há uma versão web do mesmo aplicativo Flutter. Ela permite avaliar telas, cadastrar uma conta, registrar e acompanhar reclamações com API e SQL reais. Não exige Android Studio nem Android SDK.

Na raiz, inicie a API em um terminal:

```powershell
npm.cmd run api
```

Em outro terminal:

```powershell
npm.cmd run mobile:web:build
npm.cmd run mobile:web
```

Abra **http://127.0.0.1:5200**. A página apresenta o aplicativo com largura de celular mesmo no computador. O build pode demorar na primeira execução. O script trata os caminhos acentuados e executa analyze/test antes de compilar. Para reconstruir após alterações, repita mobile:web:build e atualize a página.

O servidor Node é apenas um servidor estático/proxy local, sem regras de negócio ou banco: encaminha `/api` à mesma Web API C# na porta 5080. Nenhuma origem ampla foi liberada. Configure `MOBILE_API_URL` se usar outra porta da API, e `MOBILE_PREVIEW_PORT` se 5200 estiver ocupada. Trânsito e notificações: [configuração atual](TRANSITO-E-NOTIFICACOES.md).

No navegador a sessão fica no cookie HttpOnly administrado pelo próprio navegador; não se tenta ler esse cookie nem guardar o ticket em localStorage. No Android, permanece o armazenamento seguro nativo. Abra o painel administrativo por **localhost** e o aplicativo por **127.0.0.1** para manter as sessões separadas no mesmo navegador (cookies não separam portas).

No navegador a localização usa as permissões do navegador, e não as do Android. O preview não valida armazenamento seguro nativo, APK ou comportamento de permissões em um telefone.

## Ferramentas e execução

Flutter ≥ 3.35, Dart ≥ 3.9. SDK usado: Flutter 3.47.9 / Dart 3.13.7 instalado em `.tools/flutter` (ignorado pelo Git). Android mínimo API 23.

Para APK faltam Android Studio/Android SDK neste ambiente: instale SDK Platform, Build Tools, Command-line Tools, Platform Tools, JDK 17 compatível com Gradle, aceite licenças e configure emulador/dispositivo. Veja a [documentação oficial](https://docs.flutter.dev/platform-integration/android/setup).

```powershell
# Na raiz:
$env:Path = (Join-Path $PWD '.tools/flutter/bin') + ';' + $env:Path
cd mobile
flutter pub get
flutter analyze
flutter test --dart-define=ALLOW_HTTP=true
flutter run --dart-define=API_URL=http://10.0.2.2:5080 --dart-define=ALLOW_HTTP=true
```

Inicie a API em Development com `dotnet run --project backend/SantosConecta.API --no-restore -- --urls http://0.0.0.0:5080`. Inclua `10.0.2.2` explicitamente em `AllowedHosts`, por exemplo pela variável `AllowedHosts=localhost;127.0.0.1;10.0.2.2`. Não altere AllowedOrigins do painel para aceitar qualquer origem.

| Endereço | Significado |
|---|---|
| localhost | A máquina do cliente; no Android é o próprio aparelho/emulador |
| 10.0.2.2 | Alias do computador hospedeiro no emulador Android padrão |
| IPv4 LAN do computador | API acessada por um aparelho físico na mesma rede |

Em dispositivo físico use `API_URL=http://IP_DO_COMPUTADOR:5080`, inclua esse IP em AllowedHosts, faça a API escutar na interface da rede e permita a porta no firewall da rede local apropriada. Flutter nativo não usa CORS do navegador, mas precisa de autenticação.

O manifesto **debug** permite HTTP local. O principal bloqueia tráfego sem TLS; `ALLOW_HTTP=true` sozinho não libera um release. Distribuição exige HTTPS com certificado confiável e assinatura própria. Não desative validação de certificados. O template usa assinatura debug até configurar assinatura de distribuição.

```powershell
flutter doctor -v
flutter doctor --android-licenses
flutter build apk --release --dart-define=API_URL=https://SUA_API
# Demonstração local, somente debug:
flutter build apk --debug --dart-define=API_URL=http://10.0.2.2:5080 --dart-define=ALLOW_HTTP=true
```

## Serviços e comportamento

- AuthService: cadastro, login e logout. Senha com 12 caracteres e confirmação no cadastro, sem CPF.
- PerfilService: dados próprios, edição de nome/telefone.
- CategoriaService: sete categorias lidas da API.
- OcorrenciaService: abrir, listar próprias, detalhe e sugestão opcional de IA.
- ApiClient: timeout, erros e cookie de sessão guardado em `flutter_secure_storage` com proteção Android. Nunca salva senha. HTTP 401 apaga o ticket. Logout apaga o ticket local mesmo sem rede.
- Quatro etapas: categoria, descrição, bairro/endereço + ponto no mapa ou GPS com permissão, revisão. Protocolo aparece somente após HTTP 201.
- Início/lista têm atualização por botão ou gesto de puxar; voltar do detalhe ou nova reclamação consulta a API novamente.
- Linha do tempo marca apenas registro, estado atual e etapas com eventos reais. Não conclui Em análise quando o funcionário saltou direto para Em andamento.
- Só mostra respostas registradas como Enviado, sem observações internas ou simulações de entrega.
- Montserrat e licença locais em assets/fonts. O mapa OpenStreetMap requer internet e mantém atribuição.

## Analisador e caminho acentuado

Flutter 3.47.9 apresentou `FormatException: Unexpected end of input` no protocolo LSP com `Área de Trabalho`. O SDK local conta caracteres, em vez de bytes UTF-8, no cabeçalho da mensagem. O SDK não foi modificado.

Para conferir uma cópia idêntica em caminho temporário sem acentos, sem mover o projeto:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/check-mobile.ps1
```

O script copia fontes/testes/assets/lockfile, executa pub get/analyze/test e preserva a cópia para inspeção. Também acessa o SDK por uma junction temporária com nome ASCII para evitar a falha nativa de shaders causada pelo caminho acentuado do SDK. Não modifica o código do SDK nem apaga o original e não substitui execução em Android. Resultados: [VALIDACAO-EVOLUCAO.md](VALIDACAO-EVOLUCAO.md).
