import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:santos_conecta/services/api_client.dart';
import 'package:santos_conecta/screens/ferry_screen.dart';
import 'package:santos_conecta/models/ferry_status.dart';

void main() {
  testWidgets(
      'Travessia apresenta ausência de dados nos dois sentidos e atualiza',
      (tester) async {
    var calls = 0;
    final api = ApiClient(client: MockClient((r) async {
      expect(r.url.path, '/api/travessias/santos-guaruja');
      calls++;
      return http.Response(
          jsonEncode(
              {'aviso': 'Leitura automática indisponível.', 'fonte': 'SEMIL'}),
          200);
    }));
    await tester.pumpWidget(
        MaterialApp(theme: ThemeData.dark(), home: FerryScreen(api: api)));
    await tester.pumpAndSettle();
    expect(find.text('Santos → Guarujá'), findsOneWidget);
    expect(find.text('Guarujá → Santos'), findsOneWidget);
    expect(find.text('Tempo de espera indisponível'), findsNWidgets(2));
    expect(find.textContaining('min de espera'), findsNothing);
    await tester.ensureVisible(find.text('Atualizar consulta'));
    await tester.tap(find.text('Atualizar consulta'));
    await tester.pumpAndSettle();
    expect(calls, 2);
    await tester.pumpWidget(const SizedBox());
  });
  testWidgets('Falha da API não oferece link externo não verificado',
      (tester) async {
    final api =
        ApiClient(client: MockClient((_) async => http.Response('{}', 503)));
    await tester.pumpWidget(MaterialApp(home: FerryScreen(api: api)));
    await tester.pumpAndSettle();
    expect(find.textContaining('Não foi possível consultar'), findsOneWidget);
    expect(find.text('Consultar página externa'), findsNothing);
    expect(find.text('Consulta externa indisponível'), findsOneWidget);
    await tester.pumpWidget(const SizedBox());
  });

  Map<String, dynamic> fixture({int age = 0}) => {
        'fonte': 'Fixture identificada',
        'sentidos': [
          for (final name in ['Santos → Guarujá', 'Guarujá → Santos'])
            {
              'sentido': name,
              'esperaMinutos': name.startsWith('Santos') ? 20 : 35,
              'informadoEm': DateTime.now()
                  .toUtc()
                  .subtract(Duration(minutes: age))
                  .toIso8601String(),
              'disponivel': true,
              'desatualizado': false,
              'situacaoOperacional': 'Operação fixture',
            }
        ],
        'consultaExterna': {
          'url': 'https://metroa.com.br/santos/balsas/santos-guaruja',
          'nome': 'METROA · fonte independente',
          'disponivel': true,
          'verificadoEm': DateTime.now().toUtc().toIso8601String()
        },
      };

  test('Modelo rejeita espera inválida e link de domínio desconhecido', () {
    final json = fixture();
    (json['sentidos'] as List).first['esperaMinutos'] = -1;
    (json['consultaExterna'] as Map)['url'] = 'https://exemplo.invalid/';
    final status = FerryStatus.fromJson(json);
    expect(status.directions.first.minutes, isNull);
    expect(status.externalUrl, isNull);
    expect(ferryTime(DateTime.utc(2026, 11, 3, 15, 5)), '03/11 12:05 · SP');
  });

  testWidgets('Dados da API aparecem e link validado abre no navegador',
      (tester) async {
    Uri? opened;
    final api = ApiClient(
        client: MockClient((_) async => http.Response(
            jsonEncode(fixture()), 200,
            headers: {'content-type': 'application/json; charset=utf-8'})));
    await tester.pumpWidget(MaterialApp(
        home: FerryScreen(
            api: api,
            openExternal: (uri) async {
              opened = uri;
              return true;
            })));
    await tester.pumpAndSettle();
    expect(find.text('Atualização confirmada'), findsNWidgets(2));
    expect(find.text('Operação fixture'), findsNWidgets(2));
    await tester.ensureVisible(find.text('Consultar página externa'));
    await tester.tap(find.text('Consultar página externa'));
    expect(opened?.host, 'metroa.com.br');
    await tester.pumpWidget(const SizedBox());
  });

  testWidgets('Medição antiga é histórica e falha ao abrir tem mensagem',
      (tester) async {
    final api = ApiClient(
        client: MockClient((_) async => http.Response(
            jsonEncode(fixture(age: 60)), 200,
            headers: {'content-type': 'application/json; charset=utf-8'})));
    await tester.pumpWidget(MaterialApp(
        home: FerryScreen(api: api, openExternal: (_) async => false)));
    await tester.pumpAndSettle();
    expect(
        find.text('Histórico · não usar como tempo atual'), findsNWidgets(2));
    expect(find.text('Atualização confirmada'), findsNothing);
    await tester.ensureVisible(find.text('Consultar página externa'));
    await tester.tap(find.text('Consultar página externa'));
    await tester.pump();
    expect(
        find.textContaining('Não foi possível abrir a página'), findsOneWidget);
    await tester.pumpWidget(const SizedBox());
  });

  testWidgets('Tela estreita com fonte ampliada não transborda',
      (tester) async {
    tester.view.physicalSize = const Size(320, 800);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final api = ApiClient(
        client: MockClient((_) async => http.Response(
            jsonEncode(fixture()), 200,
            headers: {'content-type': 'application/json; charset=utf-8'})));
    await tester.pumpWidget(MaterialApp(
        home: MediaQuery(
            data: const MediaQueryData(textScaler: TextScaler.linear(1.8)),
            child: FerryScreen(api: api))));
    await tester.pumpAndSettle();
    await tester.pumpWidget(const SizedBox());
  });
}
