import 'api_client.dart';
import 'package:image_picker/image_picker.dart';

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
  Future<Map<String, dynamic>> criarComFotos(
    Map<String, dynamic> dados,
    List<XFile> fotos,
  ) async {
    final result = Map<String, dynamic>.from(
      await api.sendMultipart(
        '/api/ocorrencias/minhas/com-fotos',
        fields: {
          'idCategoria': '${dados['idCategoria']}',
          'descricao': '${dados['descricao']}',
          'bairro': '${dados['bairro']}',
          'endereco': '${dados['endereco']}',
          'localizacaoConfirmada': '${dados['localizacaoConfirmada']}',
          'latitude': '${dados['latitude']}',
          'longitude': '${dados['longitude']}',
        },
        photos: fotos,
      ),
    );
    final occurrence = Map<String, dynamic>.from(result['ocorrencia'] as Map);
    occurrence['_avisoFotos'] = result['aviso'];
    return occurrence;
  }

  Future<Map<String, dynamic>> sugerir(String descricao) async =>
      Map<String, dynamic>.from(
        await api.send(
          '/api/assistente/classificar',
          method: 'POST',
          body: {'descricao': descricao},
        ),
      );
}
