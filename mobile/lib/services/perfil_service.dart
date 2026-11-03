import 'api_client.dart';

class PerfilService {
  final ApiClient api;
  PerfilService(this.api);
  Future<Map<String, dynamic>> me() async =>
      Map<String, dynamic>.from(await api.send('/api/cidadaos/me'));
  Future<Map<String, dynamic>> editar(String nome, String telefone) async =>
      Map<String, dynamic>.from(
        await api.send(
          '/api/cidadaos/me',
          method: 'PUT',
          body: {'nome': nome, 'telefone': telefone},
        ),
      );
}
