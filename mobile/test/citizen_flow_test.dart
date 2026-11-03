import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:santos_conecta/services/api_client.dart';
import 'package:santos_conecta/services/auth_service.dart';
import 'package:santos_conecta/services/ocorrencia_service.dart';
import 'package:santos_conecta/screens/auth_screens.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  setUp(() => FlutterSecureStorage.setMockInitialValues({}));
  test('Sessão é enviada depois do login e removida quando expira', () async {
    var step = 0;
    final api = ApiClient(client: MockClient((request) async {
      step++;
      expect(request.headers['X-Santos-Client'], 'citizen');
      if (step == 1) {
        expect(jsonDecode(request.body)['senha'], 'SenhaDeTeste123!');
        return http.Response('{"nome":"Teste"}', 200, headers: {
          'set-cookie': 'SantosConecta.Citizen=ticket-seguro; HttpOnly; Path=/'
        });
      }
      expect(request.headers['Cookie'], 'SantosConecta.Citizen=ticket-seguro');
      return http.Response('{"message":"Sessão encerrada"}', 401);
    }));
    await AuthService(api).login('teste@exemplo.invalid', 'SenhaDeTeste123!');
    await expectLater(
        OcorrenciaService(api).minhas(), throwsA(isA<ApiException>()));
    const storage = FlutterSecureStorage();
    expect(await storage.read(key: 'session'), isNull);
    expect(await storage.read(key: 'senha'), isNull);
  });
  testWidgets('Cadastro recusa senhas diferentes antes de acessar a API',
      (tester) async {
    var requests = 0;
    final api = ApiClient(client: MockClient((r) async {
      requests++;
      return http.Response('{}', 500);
    }));
    await tester.pumpWidget(MaterialApp(
        home: AuthScreen(api: api, register: true, onLogin: (_) {})));
    await tester.enterText(find.byType(TextFormField).at(0), 'Cidadão Teste');
    await tester.enterText(find.byType(TextFormField).at(1), '13999999999');
    await tester.enterText(
        find.byType(TextFormField).at(2), 'teste@exemplo.invalid');
    await tester.enterText(
        find.byType(TextFormField).at(3), 'SenhaSegura12345!');
    await tester.enterText(
        find.byType(TextFormField).at(4), 'OutraSenha12345!');
    await tester.ensureVisible(find.text('Criar conta e entrar'));
    await tester.tap(find.text('Criar conta e entrar'));
    await tester.pump();
    expect(find.text('As senhas não coincidem'), findsOneWidget);
    expect(requests, 0);
    expect(tester.takeException(), isNull);
  });
}
