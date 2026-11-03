import 'package:flutter_test/flutter_test.dart';
import 'package:santos_conecta/widgets/identity.dart';
import 'package:santos_conecta/services/categoria_service.dart';

void main() {
  test('Datas inválidas não aparecem como eventos reais', () {
    expect(dateLabel(null), 'Data indisponível');
    expect(dateLabel('inválida'), 'Data indisponível');
  });
  test('Cadastro mobile oferece as sete categorias previstas', () {
    expect(CategoriaService.nomes.length, 7);
    expect(CategoriaService.nomes, contains('Buracos nas vias'));
  });
}
