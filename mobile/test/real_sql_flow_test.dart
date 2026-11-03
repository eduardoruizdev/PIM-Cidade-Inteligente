import 'dart:io';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:santos_conecta/services/api_client.dart';
import 'package:santos_conecta/services/auth_service.dart';
import 'package:santos_conecta/services/ocorrencia_service.dart';

class RealSqlBinding extends AutomatedTestWidgetsFlutterBinding {
  @override
  bool get overrideHttpClient => false;
}

void main() {
  const enabled = bool.fromEnvironment('RUN_REAL_SQL');
  if (enabled) {
    RealSqlBinding();
  } else {
    TestWidgetsFlutterBinding.ensureInitialized();
  }
  test('Flutter → HTTP real → API → SQL: status e conversa preservados',
      () async {
    // Apenas o armazenamento de cookie do dispositivo é substituído no runner sem aparelho.
    // Nenhuma resposta HTTP, consulta ou persistência é simulada.
    FlutterSecureStorage.setMockInitialValues({});
    final citizen = ApiClient(), staff = ApiClient();
    final email = Platform.environment['V2_STAFF_EMAIL']!;
    final password = Platform.environment['V2_STAFF_PASSWORD']!;
    final citizenEmail =
        'teste.flutter.${DateTime.now().microsecondsSinceEpoch}@exemplo.invalid';
    await AuthService(citizen).cadastro(
        'Cidadão Flutter SQL', citizenEmail, '13999999999', password, password);
    await AuthService(citizen).login(citizenEmail, password);
    await staff.send('/api/auth/login',
        method: 'POST', body: {'email': email, 'senha': password});
    final categories = await citizen.send('/api/categorias') as List;
    final service = OcorrenciaService(citizen);
    final created = await service.criar({
      'idCategoria': categories.first['idCategoria'],
      'descricao':
          'Teste fictício criado pelos serviços Flutter contra SQL real.',
      'bairro': 'Gonzaga',
      'endereco': 'Endereço textual de validação Flutter',
      'latitude': 0,
      'longitude': 0,
      'localizacaoConfirmada': false
    });
    final id = created['idOcorrencia'] as int;
    expect(
        (await service.minhas()).any((o) => o['idOcorrencia'] == id), isTrue);
    for (final status in [
      'Em análise',
      'Em andamento',
      'Resolvida',
      'Cancelada'
    ]) {
      await staff.send('/api/ocorrencias/$id/status',
          method: 'PUT', body: {'status': status});
      final detail = await service.detalhes(id);
      expect(detail['ocorrencia']['status'], status);
      expect(
          (detail['ocorrencia']['historico'] as List)
              .any((h) => h['descricao'] == 'Status alterado para $status.'),
          isTrue);
    }
    final path = '/api/ocorrencias/$id/mensagens';
    await staff.send(path,
        method: 'POST',
        body: {'conteudo': 'Resposta da equipe para o Flutter real.'});
    expect((await citizen.send(path) as List).first['conteudo'],
        'Resposta da equipe para o Flutter real.');
    expect(
        (await citizen.send('/api/notificacoes') as List)
            .any((n) => n['idOcorrencia'] == id),
        isTrue);
    await citizen.send('$path/lidas', method: 'POST');
    await citizen.send(path,
        method: 'POST',
        body: {'conteudo': 'Cidadão responde pelo serviço Flutter.'});
    expect((await staff.send(path) as List).last['conteudo'],
        'Cidadão responde pelo serviço Flutter.');
    final bairros = await citizen.send('/api/localizacao/bairros') as List;
    expect(bairros, contains('Gonzaga'));
    expect(bairros, isNot(contains('Guarujá')));
    final traffic = await citizen.send('/api/transito');
    expect(traffic['disponivel'], isA<bool>());
    expect(traffic['trechos'], isA<List>());
    if (traffic['disponivel'] == true) {
      expect(traffic['trechos'], isNotEmpty);
      expect(DateTime.parse(traffic['validoAte']).isAfter(DateTime.now().toUtc()), isTrue);
    } else {
      expect(traffic['trechos'], isEmpty);
    }
    await AuthService(citizen).logout();
  },
      skip: !enabled
          ? 'Exige RUN_REAL_SQL=true, API e credenciais de fixture no ambiente.'
          : false,
      timeout: const Timeout(Duration(minutes: 3)));
}
