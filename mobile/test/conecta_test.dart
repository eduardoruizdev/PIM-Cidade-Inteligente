import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:santos_conecta/screens/conecta_screen.dart';
import 'package:santos_conecta/services/api_client.dart';

void main() {
  testWidgets('Conecta envia pergunta, identifica fallback e limpa histórico',
      (tester) async {
    var deleted = false;
    final api = ApiClient(client: MockClient((r) async {
      expect(r.headers['X-Santos-Client'], 'citizen');
      if (r.method == 'DELETE') {
        deleted = true;
        return http.Response('', 204);
      }
      if (r.method == 'GET') {
        return http.Response(
            jsonEncode({
              'mensagens': [],
              'retencao': 'Fixture de histórico temporário'
            }),
            200);
      }
      expect(jsonDecode(r.body)['mensagem'], 'Vai chover amanhã?');
      return http.Response(
          jsonEncode({
            'papel': 'assistant',
            'texto':
                'Resposta de fixture identificada, sem dados meteorológicos reais.',
            'modo': 'orientativo',
            'acoes': [
              {'id': 'CLIMA', 'rotulo': 'Abrir clima'}
            ]
          }),
          200);
    }));
    await tester.pumpWidget(MaterialApp(home: ConectaScreen(api: api)));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField), 'Vai chover amanhã?');
    await tester.tap(find.byTooltip('Enviar mensagem'));
    await tester.pumpAndSettle();
    expect(find.textContaining('Resposta de fixture'), findsOneWidget);
    expect(find.text('Modo orientativo · IA indisponível ou não utilizada'),
        findsOneWidget);
    await tester.tap(find.byTooltip('Limpar histórico'));
    await tester.pumpAndSettle();
    expect(deleted, isTrue);
    expect(find.textContaining('Resposta de fixture'), findsNothing);
    expect(tester.takeException(), isNull);
  });
  for (final action in ['MINHAS_RECLAMACOES', 'TRANSITO', 'CLIMA']) {
    testWidgets('Chat devolve ação estruturada $action ao navegador local',
        (tester) async {
      Map<String, dynamic>? result;
      final api = ApiClient(
          client: MockClient((r) async => http.Response(
              jsonEncode(r.method == 'GET'
                  ? {'mensagens': [], 'retencao': 'Fixture'}
                  : {
                      'papel': 'assistant',
                      'texto': 'Fixture de navegação',
                      'modo': 'orientativo',
                      'acoes': [
                        {
                          'id': action,
                          'rotulo': 'Abrir seção',
                          'via':
                              action == 'TRANSITO' ? 'Avenida Ana Costa' : null
                        }
                      ]
                    }),
              200)));
      await tester.pumpWidget(MaterialApp(
          home: Builder(
              builder: (context) => Scaffold(
                  body: TextButton(
                      onPressed: () async {
                        result = await Navigator.push<Map<String, dynamic>>(
                            context,
                            MaterialPageRoute(
                                builder: (_) => ConectaScreen(api: api)));
                      },
                      child: const Text('Abrir Conecta'))))));
      await tester.tap(find.text('Abrir Conecta'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField), 'Ajude-me');
      await tester.tap(find.byTooltip('Enviar mensagem'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Abrir seção'));
      await tester.pumpAndSettle();
      expect(result?['id'], action);
      if (action == 'TRANSITO') expect(result?['via'], 'Avenida Ana Costa');
    });
  }
}
