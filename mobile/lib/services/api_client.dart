import 'dart:async';
import 'dart:convert';
import 'package:flutter/foundation.dart';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';
import 'package:image_picker/image_picker.dart';
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
    // Web/prod uses same-origin by default; Android emulator targets host localhost.
    defaultValue: kIsWeb ? '' : 'http://10.0.2.2:5080',
  );
  static const _storage = FlutterSecureStorage(
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
  );
  final http.Client _client;
  String? _cookie;
  ApiClient({http.Client? client}) : _client = client ?? createPlatformClient();
  String get trafficTileUrl => kIsWeb && baseUrl.isEmpty
      ? '${Uri.base.origin}/api/transito/tiles/flow/{z}/{x}/{y}'
      : '$baseUrl/api/transito/tiles/flow/{z}/{x}/{y}';
  Map<String, String> get trafficTileHeaders => {
    'X-Santos-Client': 'citizen',
    if (!kIsWeb && _cookie != null) 'Cookie': _cookie!,
  };
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
        validation = (data['errors'] as Map).values
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
      final matches = RegExp(
        r'(SantosConecta\.(?:Citizen|Auth)(?:C\d+)?=[^;,]*)',
      ).allMatches(cookies);
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

  Future<dynamic> sendMultipart(
    String path, {
    required Map<String, String> fields,
    required List<XFile> photos,
  }) async {
    final uri = kIsWeb && baseUrl.isEmpty
        ? Uri.base.resolve(path)
        : Uri.parse('$baseUrl$path');
    final request = http.MultipartRequest('POST', uri)
      ..fields.addAll(fields)
      ..headers['Accept'] = 'application/json'
      ..headers['X-Santos-Client'] = 'citizen';
    if (!kIsWeb && _cookie != null) request.headers['Cookie'] = _cookie!;
    for (final photo in photos) {
      final ext = photo.name.toLowerCase().split('.').last;
      final mime = switch (ext) {
        'jpg' || 'jpeg' => MediaType('image', 'jpeg'),
        'png' => MediaType('image', 'png'),
        'webp' => MediaType('image', 'webp'),
        _ => MediaType('application', 'octet-stream'),
      };
      request.files.add(
        http.MultipartFile.fromBytes(
          'Fotos',
          await photo.readAsBytes(),
          filename: photo.name,
          contentType: mime,
        ),
      );
    }
    http.Response response;
    try {
      response = await http.Response.fromStream(
        await _client.send(request).timeout(const Duration(seconds: 90)),
      );
    } on TimeoutException {
      throw ApiException(
        'O envio demorou. Verifique sua conexão e tente novamente.',
      );
    } on http.ClientException {
      throw ApiException(
        'Não foi possível enviar as fotografias. Confira a rede e tente novamente.',
      );
    }
    dynamic data;
    try {
      data = response.body.isEmpty ? null : jsonDecode(response.body);
    } catch (_) {
      data = null;
    }
    if (response.statusCode >= 400) {
      final detail = data is Map ? data['detail'] ?? data['message'] : null;
      throw ApiException(
        detail?.toString() ?? 'Falha no envio (${response.statusCode}).',
        response.statusCode,
      );
    }
    final cookies = response.headers['set-cookie'];
    if (!kIsWeb && cookies != null) {
      final values =
          RegExp(r'(SantosConecta\.(?:Citizen|Auth)(?:C\d+)?=[^;,]*)')
              .allMatches(cookies)
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
