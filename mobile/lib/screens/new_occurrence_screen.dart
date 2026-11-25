import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:geolocator/geolocator.dart';
import 'package:image_picker/image_picker.dart';

import '../services/api_client.dart';
import '../services/categoria_service.dart';
import '../services/ocorrencia_service.dart';

class NewOccurrenceScreen extends StatefulWidget {
  final ApiClient api;
  const NewOccurrenceScreen({super.key, required this.api});
  @override
  State<NewOccurrenceScreen> createState() => _NewOccurrenceScreenState();
}

class _NewOccurrenceScreenState extends State<NewOccurrenceScreen> {
  int step = 0;
  int? category;
  String? categoryName;
  final description = TextEditingController();
  final neighborhood = TextEditingController();
  final address = TextEditingController();
  final number = TextEditingController();
  List<String> bairros = [];
  LatLng? point;
  final map = MapController();
  bool busy = false;
  final picker = ImagePicker();
  final List<XFile> photos = [];
  String? message;
  late Future<List<Map<String, dynamic>>> categories;
  @override
  void initState() {
    super.initState();
    categories = CategoriaService(widget.api).listar();
    loadBairros();
  }

  @override
  void dispose() {
    description.dispose();
    neighborhood.dispose();
    address.dispose();
    number.dispose();
    map.dispose();
    super.dispose();
  }

  Future<void> loadBairros() async {
    try {
      final list = await widget.api.send('/api/localizacao/bairros') as List;
      if (mounted) setState(() => bairros = list.cast<String>());
    } catch (e) {
      if (mounted) setState(() => message = e.toString());
    }
  }

  Future<void> addPhotos({required bool camera}) async {
    try {
      final remaining = 5 - photos.length;
      if (remaining <= 0) {
        setState(() => message = 'Anexe no máximo cinco fotografias.');
        return;
      }
      final List<XFile> chosen;
      if (camera) {
        final photo = await picker.pickImage(source: ImageSource.camera, imageQuality: 88, maxWidth: 2000);
        chosen = photo == null ? <XFile>[] : [photo];
      } else {
        chosen = await picker.pickMultiImage(imageQuality: 88, maxWidth: 2000);
      }
      final accepted = <XFile>[];
      for (final photo in chosen.take(remaining)) {
        if (await photo.length() > 8 * 1024 * 1024) {
          setState(() => message = '${photo.name}: cada imagem deve ter até 8 MB.');
          continue;
        }
        final ext = photo.name.toLowerCase().split('.').last;
        if (!['jpg', 'jpeg', 'png', 'webp'].contains(ext)) {
          setState(() => message = '${photo.name}: use JPG, PNG ou WebP.');
          continue;
        }
        accepted.add(photo);
      }
      if (accepted.isNotEmpty && mounted) setState(() { photos.addAll(accepted); message = null; });
    } catch (_) {
      if (mounted) setState(() => message = 'Não foi possível acessar a câmera ou a galeria. Verifique a permissão do dispositivo.');
    }
  }

  String fold(String s) => s
      .toLowerCase()
      .replaceAll(RegExp('[áàâãä]'), 'a')
      .replaceAll(RegExp('[éèêë]'), 'e')
      .replaceAll(RegExp('[íìîï]'), 'i')
      .replaceAll(RegExp('[óòôõö]'), 'o')
      .replaceAll(RegExp('[úùûü]'), 'u')
      .replaceAll('ç', 'c');
  Future<void> chooseBairro() async {
    String search = '';
    final selected = await showDialog<String>(
        context: context,
        builder: (context) => StatefulBuilder(
            builder: (context, update) => AlertDialog(
                    title: Text('Bairros de Santos'),
                    content: SizedBox(
                        width: 400,
                        height: 400,
                        child: Column(children: [
                          TextField(
                              decoration:
                                  InputDecoration(labelText: 'Buscar bairro'),
                              onChanged: (v) => update(() => search = v)),
                          Expanded(
                              child: ListView(
                                  children: bairros
                                      .where(
                                          (b) => fold(b).contains(fold(search)))
                                      .map((b) => ListTile(
                                          title: Text(b),
                                          onTap: () =>
                                              Navigator.pop(context, b)))
                                      .toList()))
                        ])),
                    actions: [
                      TextButton(
                          onPressed: () => Navigator.pop(context),
                          child: Text('Cancelar'))
                    ])));
    if (selected != null && mounted) {
      setState(() {
        neighborhood.text = selected;
        point = null;
      });
    }
  }

  Future<void> searchAddress() async {
    setState(() {
      busy = true;
      message = null;
      point = null;
    });
    try {
      final query = Uri(queryParameters: {
        'bairro': neighborhood.text,
        'rua': address.text.trim(),
        'numero': number.text.trim()
      }).query;
      final results =
          await widget.api.send('/api/localizacao/buscar?$query') as List;
      if (!mounted) return;
      if (results.isEmpty) {
        setState(() => message =
            'Endereço não localizado com bairro correspondente. Você pode continuar com o endereço textual, sem ponto confirmado no mapa.');
        return;
      }
      final selected = await showDialog<Map<String, dynamic>>(
          context: context,
          builder: (c) => SimpleDialog(
              title: Text('Confirme a localização'),
              children: results
                  .map((r) => SimpleDialogOption(
                      onPressed: () =>
                          Navigator.pop(c, Map<String, dynamic>.from(r)),
                      child: Text(r['endereco'].toString())))
                  .toList()));
      if (selected != null && mounted) {
        setState(() => point = LatLng((selected['latitude'] as num).toDouble(),
            (selected['longitude'] as num).toDouble()));
        map.move(point!, 16);
      }
    } catch (e) {
      if (mounted) {
        setState(() => message =
            'Não foi possível localizar. Você pode continuar com o endereço textual.');
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> locate() async {
    setState(() => busy = true);
    try {
      if (!await Geolocator.isLocationServiceEnabled()) {
        throw Exception('Ative a localização do aparelho.');
      }
      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }
      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        throw Exception('Permissão não concedida. Informe o endereço textual.');
      }
      final p = await Geolocator.getCurrentPosition(
        locationSettings: LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: Duration(seconds: 20),
        ),
      );
      if (mounted) {
        setState(() => point = LatLng(p.latitude, p.longitude));
        map.move(point!, 16);
      }
    } catch (e) {
      if (mounted) setState(() => message = e.toString());
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> suggest() async {
    if (description.text.trim().length < 10) {
      setState(
        () => message = 'Descreva o problema com pelo menos 10 caracteres.',
      );
      return;
    }
    setState(() => busy = true);
    try {
      final s =
          await OcorrenciaService(widget.api).sugerir(description.text.trim());
      if (!mounted) return;
      if (s['disponivel'] == true) {
        final accept = await showDialog<bool>(
          context: context,
          builder: (c) => AlertDialog(
            title: Text('Sugestão da IA'),
            content: Text(
              'Categoria: ${s['categoria']}\nGravidade sugerida: ${s['gravidade']}/40\n\n${s['aviso']}',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(c, false),
                child: Text('Manter minha categoria'),
              ),
              TextButton(
                onPressed: () => Navigator.pop(c, true),
                child: Text('Confirmar categoria'),
              ),
            ],
          ),
        );
        if (accept == true && mounted) {
          final available = await categories;
          if (!mounted) return;
          if (available.any((c) => c['idCategoria'] == s['idCategoria'])) {
            setState(() {
              category = s['idCategoria'] as int?;
              categoryName = s['categoria'].toString();
            });
          }
        }
      } else {
        setState(() => message = s['aviso'].toString());
      }
    } catch (e) {
      if (mounted) setState(() => message = e.toString());
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> next() async {
    setState(() => message = null);
    if (step == 0 && category == null) {
      setState(() => message = 'Selecione a categoria.');
      return;
    }
    if (step == 1 &&
        (description.text.trim().length < 10 ||
            description.text.length > 10000)) {
      setState(
        () => message = 'Use entre 10 e 10.000 caracteres na descrição.',
      );
      return;
    }
    if (step == 2 &&
        (neighborhood.text.trim().isEmpty || address.text.trim().isEmpty)) {
      setState(
        () => message =
            'Informe o bairro e a rua. A confirmação no mapa é opcional.',
      );
      return;
    }
    if (step < 3) {
      setState(() => step++);
      return;
    }
    setState(() => busy = true);
    try {
      final dados = {
        'idCategoria': category,
        'descricao': description.text.trim(),
        'bairro': neighborhood.text.trim(),
        'endereco': [address.text.trim(), number.text.trim()]
            .where((v) => v.isNotEmpty)
            .join(', '),
        'localizacaoConfirmada': point != null,
        'latitude': point?.latitude ?? 0,
        'longitude': point?.longitude ?? 0,
      };
      final service = OcorrenciaService(widget.api);
      final o = photos.isEmpty ? await service.criar(dados) : await service.criarComFotos(dados, photos);
      if (!mounted) return;
      await showDialog<void>(
        context: context,
        barrierDismissible: false,
        builder: (c) => AlertDialog(
          title: Text('Reclamação registrada'),
          content: SelectableText(
            'Seu protocolo:\n${o['protocolo']}\n\nAcompanhe em Minhas reclamações.${o['_avisoFotos'] == null ? '' : '\n\n${o['_avisoFotos']}'}',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(c),
              child: Text('Entendi'),
            ),
          ],
        ),
      );
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) setState(() => message = e.toString());
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: Text('Nova reclamação')),
        body: Column(
          children: [
            Padding(
              padding: EdgeInsets.fromLTRB(24, 16, 24, 0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'ETAPA ${step + 1} DE 4',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: Theme.of(context).colorScheme.primary,
                    ),
                  ),
                  SizedBox(height: 10),
                  LinearProgressIndicator(value: (step + 1) / 4, minHeight: 4),
                  SizedBox(height: 16),
                ],
              ),
            ),
            Expanded(
              child: ListView(
                padding: EdgeInsets.all(24),
                children: [
                  Text(
                    [
                      'Qual é o problema?',
                      'Conte o que aconteceu',
                      'Onde fica o problema?',
                      'Revise antes de enviar',
                    ][step],
                    style: TextStyle(
                      fontWeight: FontWeight.w800,
                      fontSize: 25,
                    ),
                  ),
                  SizedBox(height: 22),
                  if (step == 0)
                    FutureBuilder<List<Map<String, dynamic>>>(
                      future: categories,
                      builder: (context, s) {
                        if (s.hasError) {
                          return Column(
                            children: [
                              Text(s.error.toString()),
                              TextButton(
                                onPressed: () => setState(
                                  () => categories =
                                      CategoriaService(widget.api).listar(),
                                ),
                                child: Text('Tentar novamente'),
                              ),
                            ],
                          );
                        }
                        if (!s.hasData) {
                          return Center(child: CircularProgressIndicator());
                        }
                        if (s.data!.isEmpty) {
                          return Text(
                            'Categorias indisponíveis. A equipe precisa aplicar o script incremental do banco.',
                          );
                        }
                        return RadioGroup<int>(
                          groupValue: category,
                          onChanged: (v) => setState(() {
                            category = v;
                            categoryName = s.data!
                                .firstWhere(
                                    (c) => c['idCategoria'] == v)['nome']
                                .toString();
                          }),
                          child: Column(
                              children: s.data!
                                  .map(
                                    (c) => Card(
                                      elevation: 0,
                                      color: category == c['idCategoria']
                                          ? Theme.of(context)
                                              .colorScheme
                                              .primaryContainer
                                          : Theme.of(context)
                                              .colorScheme
                                              .surface,
                                      child: RadioListTile<int>(
                                        title: Text(c['nome'].toString()),
                                        value: c['idCategoria'] as int,
                                      ),
                                    ),
                                  )
                                  .toList()),
                        );
                      },
                    ),
                  if (step == 1) ...[
                    TextField(
                      controller: description,
                      maxLines: 7,
                      maxLength: 10000,
                      decoration: InputDecoration(
                        labelText: 'Descrição',
                        hintText:
                            'Descreva o problema e como ele afeta o local.',
                      ),
                    ),
                    SizedBox(height: 12),
                    OutlinedButton(
                      onPressed: busy ? null : suggest,
                      child: Text('Sugerir categoria com IA'),
                    ),
                    Text(
                      'A sugestão é opcional e depende de sua confirmação. A triagem final é da equipe.',
                      style: TextStyle(fontSize: 12, height: 1.6),
                    ),
                    SizedBox(height: 20),
                    Text('Fotografias do problema (opcional, até 5)', style: TextStyle(fontWeight: FontWeight.w700)),
                    SizedBox(height: 8),
                    Wrap(spacing: 8, runSpacing: 8, children: [
                      OutlinedButton.icon(onPressed: busy ? null : () => addPhotos(camera: false), icon: Icon(Icons.photo_library_outlined), label: Text('Galeria')),
                      OutlinedButton.icon(onPressed: busy || photos.length >= 5 ? null : () => addPhotos(camera: true), icon: Icon(Icons.camera_alt_outlined), label: Text('Tirar foto')),
                    ]),
                    Wrap(spacing: 10, runSpacing: 10, children: photos.map((photo) => SizedBox(
                      width: 92, height: 92,
                      child: Stack(fit: StackFit.expand, children: [
                        ClipRRect(borderRadius: BorderRadius.circular(10), child: FutureBuilder<Uint8List>(future: photo.readAsBytes(), builder: (context, snapshot) => snapshot.hasData ? Image.memory(snapshot.data!, fit: BoxFit.cover) : Center(child: CircularProgressIndicator(strokeWidth: 2)))),
                        Positioned(right: 2, top: 2, child: IconButton.filledTonal(onPressed: busy ? null : () => setState(() => photos.remove(photo)), icon: Icon(Icons.close, size: 18), tooltip: 'Remover fotografia')),
                      ]),
                    )).toList()),
                    if (photos.isNotEmpty) Padding(padding: EdgeInsets.only(top: 8), child: Text('${photos.length}/5 fotografias selecionadas · até 8 MB cada', style: TextStyle(fontSize: 12))),
                  ],
                  if (step == 2) ...[
                    TextField(
                      controller: neighborhood,
                      readOnly: true,
                      onTap: bairros.isEmpty ? loadBairros : chooseBairro,
                      maxLength: 100,
                      decoration: InputDecoration(labelText: 'Bairro'),
                    ),
                    SizedBox(height: 16),
                    TextField(
                      controller: address,
                      onChanged: (_) => setState(() => point = null),
                      maxLength: 170,
                      decoration: InputDecoration(labelText: 'Rua ou avenida'),
                    ),
                    SizedBox(height: 16),
                    TextField(
                        controller: number,
                        maxLength: 20,
                        decoration:
                            InputDecoration(labelText: 'Número (opcional)'),
                        onChanged: (_) => setState(() => point = null)),
                    FilledButton.icon(
                        onPressed: busy ||
                                neighborhood.text.isEmpty ||
                                address.text.trim().length < 3
                            ? null
                            : searchAddress,
                        icon: Icon(Icons.search),
                        label: Text('Localizar endereço')),
                    SizedBox(height: 12),
                    OutlinedButton.icon(
                      onPressed: busy ? null : locate,
                      icon: Icon(Icons.my_location),
                      label: Text('Usar minha localização'),
                    ),
                    SizedBox(height: 12),
                    Text(
                      'Pesquise a rua e confirme uma alternativa. GPS é opcional. Sem resultado, o endereço será salvo sem marcador. Busca © OpenStreetMap / Nominatim.',
                      style: TextStyle(fontSize: 12, height: 1.6),
                    ),
                    SizedBox(height: 12),
                    SizedBox(
                      height: 290,
                      child: FlutterMap(
                        mapController: map,
                        options: MapOptions(
                          initialCenter: point ?? LatLng(-23.9595, -46.3265),
                          initialZoom: 13,
                        ),
                        children: [
                          TileLayer(
                            urlTemplate:
                                'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                            userAgentPackageName: 'br.edu.santosconecta.mobile',
                          ),
                          if (point != null)
                            MarkerLayer(
                              markers: [
                                Marker(
                                  point: point!,
                                  child: Icon(
                                    Icons.location_pin,
                                    size: 40,
                                    color:
                                        Theme.of(context).colorScheme.primary,
                                  ),
                                ),
                              ],
                            ),
                          RichAttributionWidget(
                            attributions: [
                              TextSourceAttribution(
                                  'OpenStreetMap contributors'),
                            ],
                          ),
                        ],
                      ),
                    ),
                    if (point != null)
                      Text(
                        '${point!.latitude.toStringAsFixed(6)}, ${point!.longitude.toStringAsFixed(6)}',
                        style: TextStyle(fontSize: 11),
                      ),
                  ],
                  if (step == 3) ...[
                    review('Categoria', categoryName ?? ''),
                    review('Descrição', description.text),
                    review('Local', '${address.text}\n${neighborhood.text}'),
                    review(
                      'Coordenadas',
                      point == null
                          ? 'Localização no mapa não confirmada'
                          : '${point?.latitude.toStringAsFixed(6)}, ${point?.longitude.toStringAsFixed(6)}',
                    ),
                    review('Fotografias', photos.isEmpty ? 'Nenhuma fotografia anexada' : '${photos.length} fotografia(s) serão enviadas junto com a reclamação.'),
                    Text(
                      'Você receberá um protocolo após a confirmação da API. Não envie dados pessoais sensíveis ou situações de emergência por este projeto acadêmico.',
                      style: TextStyle(fontSize: 12, height: 1.6),
                    ),
                  ],
                  if (message != null)
                    Padding(
                      padding: EdgeInsets.only(top: 20),
                      child: Text(
                        message!,
                        style: TextStyle(
                          color: Theme.of(context).colorScheme.error,
                        ),
                      ),
                    ),
                ],
              ),
            ),
            SafeArea(
              top: false,
              child: Padding(
                padding: EdgeInsets.all(24),
                child: Row(
                  children: [
                    if (step > 0)
                      TextButton(
                        onPressed: busy ? null : () => setState(() => step--),
                        child: Text('Voltar'),
                      ),
                    SizedBox(width: 12),
                    Expanded(
                      child: FilledButton(
                        onPressed: busy ? null : next,
                        child: busy
                            ? Row(mainAxisSize: MainAxisSize.min, children: [CircularProgressIndicator(strokeWidth: 2), SizedBox(width: 10), Text('Enviando reclamação...')])
                            : Text(step == 3 ? 'Enviar reclamação' : 'Continuar'),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      );
  Widget review(String label, String value) => Padding(
        padding: EdgeInsets.only(bottom: 24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              label,
              style: TextStyle(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                  fontSize: 12),
            ),
            SizedBox(height: 8),
            Text(value, style: TextStyle(fontSize: 15, height: 1.6)),
          ],
        ),
      );
}
