# Santos Conecta · Aplicativo do cidadão

Flutter / Material 3 / Android, consumindo a API C# existente. Projeto acadêmico fictício, sem vínculo com a Prefeitura de Santos.

## Execução

Para testar no navegador sem Android Studio, na raiz do repositório use `npm.cmd run mobile:web:build` e depois `npm.cmd run mobile:web`. Com a API na porta 5080, abra http://127.0.0.1:5200.

Instruções: [docs/FLUTTER.md](../docs/FLUTTER.md).

```powershell
flutter pub get
flutter analyze
flutter test --dart-define=ALLOW_HTTP=true
flutter run --dart-define=API_URL=http://10.0.2.2:5080 --dart-define=ALLOW_HTTP=true
```

Sem Android SDK não é possível gerar APK. As diferenças entre localhost, 10.0.2.2 e o IP da rede, além de TLS e armazenamento da sessão, estão no guia de execução.
