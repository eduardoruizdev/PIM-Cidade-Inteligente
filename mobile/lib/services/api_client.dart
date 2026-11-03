import 'dart:async';
import 'dart:convert';
import 'package:flutter/foundation.dart';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import 'platform_client.dart';

class ApiException implements Exception {
  final String message;
  final int status;
  ApiException(this.message, [this.status = 0]);
  @override
  String toString() => message;
}

class ApiClient {
  static const baseUrl = String.fromEnvironment(
    'API_URL',
    defaultValue: 'http://10.0.2.2:5080',
  );
  static const _storage = FlutterSecureStorage(
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
  );
  final http.Client _client;
  String? _cookie;
  ApiClient({http.Client? client}) : _client = client ?? createPlatformClient();
  Future<void> restore() async {
    if (kIsWeb) return;
    _cookie = await _storage.read(key: 'session');
  }

  Future<void> clear() async {
    _cookie = null;
    if (!kIsWeb) await _storage.delete(key: 'session');
  }

  Future<dynamic> send(
    String path, {
    String method = 'GET',
    Map<String, dynamic>? body,
  }) async {
    final uri = kIsWeb && baseUrl.isEmpty
        ? Uri.base.resolve(path)
        : Uri.parse('$baseUrl$path');
    if (uri.scheme != 'https' &&
        !(const bool.fromEnvironment('ALLOW_HTTP', defaultValue: false))) {
      throw ApiException(
        'Use HTTPS ou ALLOW_HTTP=true somente no desenvolvimento local.',
      );
    }
    final request = http.Request(method, uri)
      ..headers['Accept'] = 'application/json'
      ..headers['X-Santos-Client'] = 'citizen';
    if (!kIsWeb && _cookie != null) request.headers['Cookie'] = _cookie!;
    if (body != null) {
      request.headers['Content-Type'] = 'application/json';
      request.body = jsonEncode(body);
    }
    http.Response response;
    try {
      response = await http.Response.fromStream(
        await _client.send(request).timeout(const Duration(seconds: 35)),
      );
    } on TimeoutException {
      throw ApiException('A conexão demorou. Tente novamente.');
    } on http.ClientException {
      throw ApiException(
        'Não foi possível acessar a API. Confira a rede e o endereço configurado.',
      );
    }
    dynamic data;
    try {
      data = response.body.isEmpty ? null : jsonDecode(response.body);
    } catch (_) {
      data = null;
    }
    if (response.statusCode >= 400) {
      if (response.statusCode == 401) await clear();
      String? validation;
      if (data is Map && data['errors'] is Map) {
        validation = (data['errors'] as Map)
            .values
            .expand((v) => v is List ? v : [v])
            .join(' ');
      }
      throw ApiException(
        data is Map
            ? (data['detail'] ??
                    data['message'] ??
                    validation ??
                    'Não foi possível concluir a operação.')
                .toString()
            : 'Falha na API (${response.statusCode}).',
        response.statusCode,
      );
    }
    // A sessão ASP.NET é guardada no armazenamento seguro; a senha nunca é persistida.
    final cookies = response.headers['set-cookie'];
    if (!kIsWeb && cookies != null) {
      final matches =
          RegExp(r'(SantosConecta\.(?:Citizen|Auth)(?:C\d+)?=[^;,]*)')
              .allMatches(cookies);
      final values = matches
          .map((m) => m.group(1)!)
          .where((v) => !v.endsWith('='))
          .toList();
      if (values.isNotEmpty) {
        _cookie = values.join('; ');
        await _storage.write(key: 'session', value: _cookie);
      }
    }
    return data;
  }
}
