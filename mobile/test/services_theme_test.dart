import 'dart:convert';
import 'dart:ui' as ui;
import 'package:flutter/foundation.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:latlong2/latlong.dart';
import 'package:santos_conecta/services/appearance.dart';
import 'package:santos_conecta/services/api_client.dart';
import 'package:santos_conecta/screens/nearby_services_screen.dart';

void main() {
  testWidgets('Marcador abre detalhes sem consultar fonte ao mover mapa',
      (tester) async {
    var calls = 0;
    final image = (await tester.runAsync(()=>createTestImage()))!;
    final api = ApiClient(client: MockClient((r) async {
      calls++;
      if (r.url.path.endsWith('categorias')) {
        return http.Response('[{"id":"ubs","nome":"UBS"}]', 200);
      }
      return http.Response(
          jsonEncode({
            'itens': [
              {
                'id': 'santos-1',
                'nome': 'Marcador Fixture',
                'categoria': 'ubs',
                'latitude': -23.96,
                'longitude': -46.333,
                'fonte': 'Fixture',
                'consultadoEm': DateTime.now().toUtc().toIso8601String()
              }
            ],
            'total': 1,
            'bairros': [],
            'consultadoEm': DateTime.now().toUtc().toIso8601String(),
            'desatualizado': false
          }),
          200);
    }));
    await tester.pumpWidget(MaterialApp(
        home: NearbyServicesScreen(api: api, tiles: _Tiles(image))));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Ver mapa'));
    await tester.pumpAndSettle();
    final button = find.descendant(
        of: find.byTooltip('Marcador Fixture'),
        matching: find.byType(IconButton));
    expect(button, findsOneWidget);
    tester.widget<IconButton>(button).onPressed!();
    await tester.pumpAndSettle();
    expect(find.textContaining('Atualização do cadastro: Não informado'),
        findsOneWidget);
    expect(calls, 2);
  });
  setUp(() => FlutterSecureStorage.setMockInitialValues({}));
  test('Aparência persiste os três modos em nova instância', () async {
    for (final mode in ThemeMode.values) {
      final first = Appearance();
      expect(await first.select(mode), isTrue);
      final restarted = Appearance();
      await restarted.load();
      expect(restarted.mode, mode);
      first.dispose();
      restarted.dispose();
    }
  });
  testWidgets('Automático acompanha alteração do tema do sistema',
      (tester) async {
    tester.platformDispatcher.platformBrightnessTestValue = Brightness.light;
    addTearDown(tester.platformDispatcher.clearPlatformBrightnessTestValue);
    await tester.pumpWidget(MaterialApp(
        theme: santosTheme(Brightness.light),
        darkTheme: santosTheme(Brightness.dark),
        themeMode: ThemeMode.system,
        home: Builder(
            builder: (context) => Text(Theme.of(context).brightness.name))));
    expect(find.text('light'), findsOneWidget);
    tester.platformDispatcher.platformBrightnessTestValue = Brightness.dark;
    await tester.pumpAndSettle();
    expect(find.text('dark'), findsOneWidget);
  });
  test('Rotas usam destino verificado e não incluem origem GPS', () {
    final uri = serviceDirections(
        {'nome': 'Fixture', 'latitude': -23.96, 'longitude': -46.33});
    expect(uri.host, 'www.google.com');
    expect(uri.queryParameters['destination'], '-23.96,-46.33');
    expect(uri.queryParameters.containsKey('origin'), isFalse);
    expect(serviceValue(null), 'Não informado');
  });
  for (final authorized in [false, true]) {
    testWidgets(
        'Pesquisa funciona com localização ${authorized ? 'autorizada' : 'negada'}',
        (tester) async {
      tester.view.physicalSize=const Size(480,1200);tester.view.devicePixelRatio=1;
      addTearDown(tester.view.resetPhysicalSize);addTearDown(tester.view.resetDevicePixelRatio);
      var locationRequests = 0;
      var coordinateQuery = false;
      Uri? opened;
      final api = ApiClient(client: MockClient((r) async {
        if (r.url.path.endsWith('categorias')) {
          return http.Response(
              jsonEncode([
                {'id': 'ubs', 'nome': 'UBS e policlínicas'}
              ]),
              200);
        }
        if (r.url.queryParameters.containsKey('latitude')) {
          coordinateQuery = true;
        }
        return http.Response(
            jsonEncode({
              'itens': [
                {
                  'id': 'santos-1',
                  'nome': 'Policlínica Fixture',
                  'categoria': 'ubs',
                  'endereco': 'Endereço de fixture',
                  'bairro': 'Centro',
                  'telefone': null,
                  'horario': null,
                  'latitude': -23.96,
                  'longitude': -46.33,
                  'fonte': 'Fixture identificada',
                  'consultadoEm': DateTime.now().toUtc().toIso8601String()
                }
              ],
              'total': 1,
              'bairros': ['Centro'],
              'consultadoEm': DateTime.now().toUtc().toIso8601String(),
              'desatualizado': false
            }),
            200);
      }));
      await tester.pumpWidget(MaterialApp(
          theme: santosTheme(Brightness.dark),
          home: NearbyServicesScreen(
              api: api,
              locate: () async {
                locationRequests++;
                return authorized ? const LatLng(-23.96, -46.33) : null;
              },
              open: (uri) async {
                opened = uri;
                return true;
              })));
      await tester.pumpAndSettle();
      expect(locationRequests, 0);
      await tester.tap(find.text('Usar minha localização'));
      await tester.pumpAndSettle();
      expect(locationRequests, 1);
      expect(coordinateQuery, authorized);
      if (!authorized) {
        expect(find.textContaining('Escolha um bairro'), findsOneWidget);
      }
      await tester.enterText(find.byType(TextField), 'Policlínica');
      await tester.pump(const Duration(milliseconds: 400));
      await tester.pumpAndSettle();
      expect(find.text('Policlínica Fixture'), findsOneWidget);
      await tester.ensureVisible(find.text('Como chegar'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Como chegar'));
      await tester.pumpAndSettle();
      expect(opened?.host, 'www.google.com');
      await tester.ensureVisible(find.text('Detalhes'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Detalhes'));
      await tester.pumpAndSettle();
      expect(find.textContaining('Telefone: Não informado'), findsWidgets);
      expect(tester.takeException(), isNull);
    });
  }
}

class _Tiles extends TileProvider {
  final ui.Image image;
  _Tiles(this.image);
  @override
  ImageProvider getImage(TileCoordinates coordinates, TileLayer options) =>
      _TileImage(image);
}

class _TileImage extends ImageProvider<_TileImage> {
  final ui.Image image;
  const _TileImage(this.image);
  @override
  Future<_TileImage> obtainKey(ImageConfiguration configuration) =>
      SynchronousFuture(this);
  @override
  ImageStreamCompleter loadImage(_TileImage key, ImageDecoderCallback decode) =>
      OneFrameImageStreamCompleter(SynchronousFuture(ImageInfo(image: image)));
}
