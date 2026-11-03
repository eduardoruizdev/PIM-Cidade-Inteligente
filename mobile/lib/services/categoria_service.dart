import 'api_client.dart';

class CategoriaService {
  final ApiClient api;
  CategoriaService(this.api);
  static const nomes = [
    'Buracos nas vias',
    'Iluminação pública',
    'Coleta de lixo',
    'Calçadas',
    'Drenagem',
    'Sinalização',
    'Outros',
  ];
  Future<List<Map<String, dynamic>>> listar() async =>
      (await api.send('/api/categorias') as List)
          .map((e) => Map<String, dynamic>.from(e))
          .where((e) => nomes.contains(e['nome']))
          .toList();
}
