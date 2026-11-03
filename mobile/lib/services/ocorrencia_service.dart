import 'api_client.dart';

class OcorrenciaService {
  final ApiClient api;
  OcorrenciaService(this.api);
  Future<List<Map<String, dynamic>>> minhas() async =>
      (await api.send('/api/ocorrencias/minhas') as List)
          .map((e) => Map<String, dynamic>.from(e))
          .toList();
  Future<Map<String, dynamic>> detalhes(int id) async =>
      Map<String, dynamic>.from(await api.send('/api/ocorrencias/$id'));
  Future<Map<String, dynamic>> criar(Map<String, dynamic> dados) async =>
      Map<String, dynamic>.from(
        await api.send('/api/ocorrencias', method: 'POST', body: dados),
      );
  Future<Map<String, dynamic>> sugerir(String descricao) async =>
      Map<String, dynamic>.from(
        await api.send(
          '/api/assistente/classificar',
          method: 'POST',
          body: {'descricao': descricao},
        ),
      );
}
