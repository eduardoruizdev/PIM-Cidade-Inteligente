import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'api_client.dart';

class WeatherPush {
  static StreamSubscription<String>? _refresh;
  static final _opened = StreamController<String>.broadcast();
  static Stream<String> get opened => _opened.stream;
  static bool _listening = false;
  static Future<void> initialize() async {
    if (kIsWeb || !const bool.fromEnvironment('FIREBASE_ENABLED')) return;
    if (Firebase.apps.isEmpty) {
      // Configuração nativa do aplicativo; nenhuma credencial de servidor no Dart.
      await Firebase.initializeApp();
    }
    if (!_listening) {
      _listening = true;
      FirebaseMessaging.onMessageOpenedApp
          .listen((m) => _opened.add(m.data['alertId']?.toString() ?? ''));
      FirebaseMessaging.onMessage
          .listen((m) => _opened.add(m.data['alertId']?.toString() ?? ''));
      final initial = await FirebaseMessaging.instance.getInitialMessage();
      if (initial != null) {
        _opened.add(initial.data['alertId']?.toString() ?? '');
      }
    }
  }

  static Future<void> restore(ApiClient api) async {
    if (kIsWeb || !const bool.fromEnvironment('FIREBASE_ENABLED')) return;
    await initialize();
    final p = await api.send('/api/clima/preferencias');
    final settings = await FirebaseMessaging.instance.getNotificationSettings();
    if (p['habilitada'] == true &&
        (settings.authorizationStatus == AuthorizationStatus.authorized ||
            settings.authorizationStatus == AuthorizationStatus.provisional)) {
      await configure(api, true);
    }
  }

  static Future<void> configure(ApiClient api, bool enabled) async {
    if (!enabled) {
      await _refresh?.cancel();
      _refresh = null;
      await api.send('/api/clima/preferencias',
          method: 'PUT', body: {'habilitada': false});
      return;
    }
    if (kIsWeb || !const bool.fromEnvironment('FIREBASE_ENABLED')) {
      throw StateError('Firebase não configurado para esta plataforma.');
    }
    await initialize();
    final permission = await FirebaseMessaging.instance.requestPermission();
    if (permission.authorizationStatus != AuthorizationStatus.authorized &&
        permission.authorizationStatus != AuthorizationStatus.provisional) {
      throw StateError('Permissão negada.');
    }
    final token = await FirebaseMessaging.instance.getToken();
    if (token == null) {
      throw StateError('Token indisponível.');
    }
    await api.send('/api/clima/preferencias',
        method: 'PUT', body: {'habilitada': true, 'token': token});
    await _refresh?.cancel();
    _refresh = FirebaseMessaging.instance.onTokenRefresh.listen((token) async {
      try {
        await api.send('/api/clima/preferencias',
            method: 'PUT', body: {'habilitada': true, 'token': token});
      } catch (_) {
        /* Reativar pela Central de Alertas após recuperar a sessão. */
      }
    });
  }
}
