import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:santos_conecta/services/api_client.dart';
import 'package:santos_conecta/screens/weather_screen.dart';
import 'package:santos_conecta/screens/weather_alerts_screen.dart';

void main() {
  testWidgets('Clima apresenta dados da API e não transborda em tela estreita',
      (tester) async {
    tester.view.physicalSize = const Size(320, 900);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final timestamp = DateTime.now().millisecondsSinceEpoch ~/ 1000 + 3600;
    final api = ApiClient(
        client: MockClient((r) async => http.Response(
            jsonEncode({
              'fonte': 'Fixture de teste identificada',
              'ultimaAtualizacao': DateTime.now().toUtc().toIso8601String(),
              'validoAte': DateTime.now()
                  .toUtc()
                  .add(const Duration(minutes: 15))
                  .toIso8601String(),
              'dados': {
                'current': {
                  'temperature_2m': 25,
                  'apparent_temperature': 27,
                  'relative_humidity_2m': 80,
                  'wind_speed_10m': 12,
                  'weather_code': 61,
                  'time': timestamp
                },
                'hourly': {
                  'time': [timestamp],
                  'temperature_2m': [25],
                  'precipitation_probability': [70],
                  'weather_code': [61]
                },
                'daily': {
                  'time': [timestamp],
                  'weather_code': [61],
                  'precipitation_probability_max': [70],
                  'temperature_2m_min': [22],
                  'temperature_2m_max': [28]
                }
              }
            }),
            200)));
    await tester.pumpWidget(MaterialApp(home: WeatherScreen(api: api)));
    await tester.pumpAndSettle();
    expect(find.text('25 °C'), findsWidgets);
    expect(find.text('Sensação térmica: 27 °C'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
  testWidgets('Falha de alertas não afirma ausência de riscos', (tester) async {
    final api =
        ApiClient(client: MockClient((r) async => http.Response('{}', 503)));
    await tester.pumpWidget(MaterialApp(home: WeatherAlertsScreen(api: api)));
    await tester.pumpAndSettle();
    expect(find.text('Não foi possível consultar os avisos. Tente atualizar.'),
        findsOneWidget);
    expect(find.textContaining('Nenhum aviso disponível'), findsNothing);
  });
}
