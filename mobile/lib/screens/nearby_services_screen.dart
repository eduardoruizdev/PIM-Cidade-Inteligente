import 'dart:async';
import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:geolocator/geolocator.dart';
import 'package:url_launcher/url_launcher.dart';
import '../services/api_client.dart';

String serviceValue(dynamic value) =>
    value == null || value.toString().trim().isEmpty
        ? 'Não informado'
        : value.toString();
Uri serviceDirections(Map<String, dynamic> s) =>
    Uri.https('www.google.com', '/maps/dir/', {
      'api': '1',
      'destination': s['latitude'] != null && s['longitude'] != null
          ? '${s['latitude']},${s['longitude']}'
          : '${s['nome']}, ${s['endereco'] ?? ''}, Santos SP'
    });
IconData serviceIcon(String? category) => switch (category) {
      'ubs' => Icons.local_hospital,
      'upa' => Icons.emergency,
      'hospital' => Icons.local_hospital_outlined,
      'escola' => Icons.school,
      'assistencia' => Icons.diversity_3,
      'biblioteca' => Icons.local_library,
      'cultura-esporte' => Icons.sports_soccer,
      _ => Icons.account_balance
    };
Color serviceColor(String? category) => switch (category) {
      'ubs' => Colors.teal,
      'upa' => Colors.red,
      'hospital' => Colors.purple,
      'escola' => Colors.blue,
      'assistencia' => Colors.deepOrange,
      'biblioteca' => Colors.indigo,
      'cultura-esporte' => Colors.green,
      _ => Colors.brown
    };

class NearbyServicesScreen extends StatefulWidget {
  final ApiClient api;
  final Future<LatLng?> Function()? locate;
  final Future<bool> Function(Uri)? open;
  final TileProvider? tiles;
  const NearbyServicesScreen(
      {super.key, required this.api, this.locate, this.open, this.tiles});
  @override
  State<NearbyServicesScreen> createState() => _NearbyServicesScreenState();
}

class _NearbyServicesScreenState extends State<NearbyServicesScreen> {
  final search = TextEditingController();
  final map = MapController();
  Timer? debounce;
  List<dynamic> items = [], categories = [];
  List<String> bairros = [];
  String? category, bairro, error, notice;
  String source = '';
  bool loading = true, mapView = false, locating = false;
  LatLng? position;
  double zoom = 13;
  int generation = 0, total = 0;
  Map<String, dynamic>? focused;
  @override
  void initState() {
    super.initState();
    load();
  }

  @override
  void dispose() {
    debounce?.cancel();
    search.dispose();
    map.dispose();
    super.dispose();
  }

  Future<void> load({bool more = false}) async {
    final request = ++generation;
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final query = Uri(queryParameters: {
        'limite': '100',
        'inicio': more ? '${items.length}' : '0',
        if (category != null) 'categoria': category!,
        if (bairro != null) 'bairro': bairro!,
        if (search.text.trim().isNotEmpty) 'texto': search.text.trim(),
        if (position != null) 'latitude': '${position!.latitude}',
        if (position != null) 'longitude': '${position!.longitude}'
      }).query;
      final results = await Future.wait([
        widget.api.send('/api/servicos-publicos?$query'),
        if (categories.isEmpty)
          widget.api.send('/api/servicos-publicos/categorias')
      ]);
      if (!mounted || request != generation) return;
      final data = results[0];
      setState(() {
        items = more ? [...items, ...data['itens']] : data['itens'] as List;
        total = (data['total'] as num).toInt();
        bairros = (data['bairros'] as List).cast<String>();
        if (results.length > 1) categories = results[1] as List;
        source =
            'Fonte: Prefeitura de Santos · consulta: ${DateTime.parse(data['consultadoEm']).toLocal()}${data['desatualizado'] == true ? ' · Cópia desatualizada: fonte indisponível' : ''}';
      });
    } catch (_) {
      if (mounted && request == generation) {
        setState(() => error =
            'Não foi possível consultar os serviços. Verifique sua conexão e tente novamente.');
      }
    } finally {
      if (mounted && request == generation) setState(() => loading = false);
    }
  }

  Future<void> locate() async {
    setState(() => locating = true);
    try {
      LatLng? p;
      if (widget.locate != null) {
        p = await widget.locate!();
      } else if (await Geolocator.isLocationServiceEnabled()) {
        var permission = await Geolocator.checkPermission();
        if (permission == LocationPermission.denied) {
          permission = await Geolocator.requestPermission();
        }
        if (permission == LocationPermission.whileInUse ||
            permission == LocationPermission.always) {
          final value = await Geolocator.getCurrentPosition(
              locationSettings:
                  const LocationSettings(timeLimit: Duration(seconds: 15)));
          p = LatLng(value.latitude, value.longitude);
        }
      }
      if (!mounted) return;
      if (p == null) {
        setState(() => notice =
            'Localização não disponível ou não autorizada. Escolha um bairro ou pesquise pelo nome.');
        return;
      }
      setState(() => position = p);
      if (mapView) map.move(p, 13);
      await load();
    } catch (_) {
      if (mounted) {
        setState(() => notice =
            'Não foi possível obter a localização. Use a seleção manual de bairro.');
      }
    } finally {
      if (mounted) setState(() => locating = false);
    }
  }

  String categoryName(dynamic id) =>
      categories
          .where((c) => c['id'] == id)
          .map((c) => c['nome'].toString())
          .firstOrNull ??
      'Não informado';
  Future<void> directions(Map<String, dynamic> s) async {
    try {
      final ok = await (widget.open?.call(serviceDirections(s)) ??
          launchUrl(serviceDirections(s),
              mode: LaunchMode.externalApplication));
      if (!ok) throw StateError('Navegação indisponível');
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
            content:
                Text('Não foi possível abrir o aplicativo de navegação.')));
      }
    }
  }

  void focus(Map<String, dynamic> s) {
    if (s['latitude'] == null || s['longitude'] == null) return;
    final was = mapView;
    setState(() {
      focused = s;
      mapView = true;
    });
    if (was) {
      map.move(
          LatLng((s['latitude'] as num).toDouble(),
              (s['longitude'] as num).toDouble()),
          16);
    }
  }

  void details(Map<String, dynamic> s) => showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (context) => SafeArea(
          child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(s['nome'],
                        style: Theme.of(context).textTheme.titleLarge),
                    Text(categoryName(s['categoria'])),
                    const SizedBox(height: 16),
                    Text(
                        'Endereço: ${serviceValue(s['endereco'])}\nBairro: ${serviceValue(s['bairro'])}\nTelefone: ${serviceValue(s['telefone'])}\nHorário: ${serviceValue(s['horario'])}'),
                    const SizedBox(height: 16),
                    Text(
                        'Fonte: ${s['fonte']}\nConsulta: ${s['consultadoEm']}\nAtualização do cadastro: ${serviceValue(s['atualizacaoFonte'])}'),
                    const Text(
                        'Confirme o atendimento na unidade. Distâncias são aproximadas em linha reta.'),
                    const SizedBox(height: 16),
                    OutlinedButton(
                        onPressed: s['latitude'] == null
                            ? null
                            : () {
                                Navigator.pop(context);
                                focus(s);
                              },
                        child: const Text('Ver no mapa')),
                    FilledButton(
                        onPressed: () => directions(s),
                        child: const Text('Como chegar')),
                  ]))));
  List<Marker> markers() {
    final groups = <String, List<Map<String, dynamic>>>{};
    final cell = 360 / math.pow(2, zoom) * .16;
    for (final raw in items) {
      final s = Map<String, dynamic>.from(raw);
      if (s['latitude'] == null || s['longitude'] == null) continue;
      final key =
          '${((s['latitude'] as num) / cell).floor()}:${((s['longitude'] as num) / cell).floor()}';
      groups.putIfAbsent(key, () => []).add(s);
    }
    return groups.values.map((group) {
      final lat = group.fold<double>(0, (v, s) => v + (s['latitude'] as num)) /
          group.length;
      final lon = group.fold<double>(0, (v, s) => v + (s['longitude'] as num)) /
          group.length;
      return Marker(
          point: LatLng(lat, lon),
          width: 48,
          height: 48,
          child: Tooltip(
              message: group.length == 1
                  ? group.first['nome']
                  : '${group.length} serviços',
              child: IconButton.filled(
                  style: IconButton.styleFrom(
                      backgroundColor: serviceColor(group.first['categoria']),
                      foregroundColor: Colors.white),
                  onPressed: () {
                    if (group.length == 1) {
                      details(group.first);
                    } else if (zoom < 17) {
                      map.move(LatLng(lat, lon), zoom + 2);
                    } else {
                      showModalBottomSheet<void>(
                          context: context,
                          builder: (context) => ListView(
                              children: group
                                  .map((s) => ListTile(
                                      title: Text(s['nome']),
                                      onTap: () {
                                        Navigator.pop(context);
                                        details(s);
                                      }))
                                  .toList()));
                    }
                  },
                  icon: group.length == 1
                      ? Icon(serviceIcon(group.first['categoria']))
                      : Text('${group.length}',
                          style: const TextStyle(
                              color: Colors.white,
                              fontWeight: FontWeight.bold)))));
    }).toList();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
      appBar: AppBar(title: const Text('Serviços Próximos')),
      body: ListView(padding: const EdgeInsets.all(20), children: [
        TextField(
            controller: search,
            maxLength: 100,
            decoration: const InputDecoration(
                labelText: 'Pesquisar por nome',
                prefixIcon: Icon(Icons.search)),
            onChanged: (_) {
              debounce?.cancel();
              debounce = Timer(const Duration(milliseconds: 350), () => load());
            }),
        DropdownButtonFormField<String>(
            isExpanded: true,
            initialValue: category,
            decoration: const InputDecoration(labelText: 'Categoria'),
            items: [
              const DropdownMenuItem<String>(
                  value: null, child: Text('Todas as categorias')),
              ...categories.map((c) => DropdownMenuItem(
                  value: c['id'].toString(), child: Text(c['nome'])))
            ],
            onChanged: (v) {
              setState(() => category = v);
              load();
            }),
        const SizedBox(height: 12),
        DropdownButtonFormField<String>(
            isExpanded: true,
            initialValue: bairro,
            decoration: const InputDecoration(labelText: 'Bairro'),
            items: [
              const DropdownMenuItem<String>(
                  value: null, child: Text('Todos os bairros')),
              ...bairros.map((b) => DropdownMenuItem(value: b, child: Text(b)))
            ],
            onChanged: (v) {
              setState(() => bairro = v);
              load();
            }),
        Wrap(spacing: 8, children: [
          TextButton.icon(
              onPressed: locating ? null : locate,
              icon: const Icon(Icons.my_location),
              label:
                  Text(locating ? 'Localizando…' : 'Usar minha localização')),
          TextButton(
              onPressed: () => setState(() => mapView = !mapView),
              child: Text(mapView ? 'Ver lista' : 'Ver mapa')),
          TextButton(
              onPressed: loading ? null : () => load(),
              child: const Text('Atualizar'))
        ]),
        if (notice != null) Text(notice!),
        if (position != null)
          const Text(
              'Ordenado pela distância aproximada. Localização não é armazenada no cadastro.'),
        if (loading) const LinearProgressIndicator(),
        if (error != null)
          Text(error!,
              style: TextStyle(color: Theme.of(context).colorScheme.error)),
        if (mapView)
          SizedBox(
              height: 360,
              child: FlutterMap(
                  mapController: map,
                  options: MapOptions(
                      initialCenter: position ?? const LatLng(-23.96, -46.333),
                      initialZoom: 13,
                      onMapReady: () {
                        if (focused != null) {
                          map.move(
                              LatLng((focused!['latitude'] as num).toDouble(),
                                  (focused!['longitude'] as num).toDouble()),
                              16);
                        }
                      },
                      onMapEvent: (event) {
                        if (event is MapEventMoveEnd) {
                          setState(() => zoom = event.camera.zoom);
                        }
                      }),
                  children: [
                    TileLayer(
                        tileProvider: widget.tiles,
                        urlTemplate:
                            'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                        userAgentPackageName: 'br.edu.santosconecta.mobile'),
                    MarkerLayer(markers: markers()),
                    const RichAttributionWidget(attributions: [
                      TextSourceAttribution('© OpenStreetMap contributors')
                    ])
                  ])),
        if (mapView)
          const Text(
              'Mapa de base claro preservado para manter legíveis os marcadores. Unidades sem coordenadas aparecem somente na lista.'),
        Text('$total unidades encontradas · ${items.length} exibidas'),
        if (!loading && error == null && items.isEmpty)
          const Text('Nenhum serviço encontrado com esses filtros.'),
        ...items.map((raw) {
          final s = Map<String, dynamic>.from(raw);
          return Card(
              child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        ListTile(
                            contentPadding: EdgeInsets.zero,
                            leading: Icon(serviceIcon(s['categoria']),
                                color: serviceColor(s['categoria'])),
                            title: Text(s['nome']),
                            subtitle: Text(categoryName(s['categoria'])),
                            onTap: () => details(s)),
                        Text(
                            '${serviceValue(s['endereco'])}\nBairro: ${serviceValue(s['bairro'])}\nTelefone: ${serviceValue(s['telefone'])}\nHorário: ${serviceValue(s['horario'])}\nDistância: ${s['distanciaKm'] is num ? '${(s['distanciaKm'] as num).toStringAsFixed(1)} km (aprox.)' : 'Não informado'}'),
                        Wrap(spacing: 8, children: [
                          TextButton(
                              onPressed: () => details(s),
                              child: const Text('Detalhes')),
                          TextButton(
                              onPressed:
                                  s['latitude'] == null ? null : () => focus(s),
                              child: const Text('Ver no mapa')),
                          TextButton(
                              onPressed: () => directions(s),
                              child: const Text('Como chegar'))
                        ])
                      ])));
        }),
        if (items.length < total)
          TextButton(
              onPressed: loading ? null : () => load(more: true),
              child: const Text('Carregar mais')),
        Text(source, style: Theme.of(context).textTheme.bodySmall),
      ]));
}
