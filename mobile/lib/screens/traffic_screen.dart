import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import '../services/api_client.dart';
import '../widgets/identity.dart';

class TrafficScreen extends StatefulWidget {
  final ApiClient api;
  final String? initialRoad;
  const TrafficScreen({super.key, required this.api, this.initialRoad});
  @override
  State<TrafficScreen> createState() => _TrafficScreenState();
}

class _TrafficScreenState extends State<TrafficScreen>
    with WidgetsBindingObserver {
  final map = MapController();
  late Future<dynamic> data;
  String region = 'Orla';
  LatLng center = LatLng(-23.972, -46.327);
  Timer? timer;
  bool active = true;
  int ticks = 0;
  final roadInput = TextEditingController(text: 'Avenida Ana Costa');
  List<List<LatLng>> roadSegments = [];
  String? selectedRoad;
  String? roadMessage;
  bool roadLoading = false;
  int roadRequest = 0;
  Future<void> selectRoad() async {
    final generation = ++roadRequest;
    setState(() {
      roadLoading = true;
      roadMessage = null;
      roadSegments = [];
    });
    try {
      final result = await widget.api.send(
          '/api/transito/geometria?nome=${Uri.encodeComponent(roadInput.text.trim())}');
      if (!mounted || generation != roadRequest) return;
      final segments = (result['segmentos'] as List)
          .map((s) => (s as List)
              .map((p) => LatLng((p['latitude'] as num).toDouble(),
                  (p['longitude'] as num).toDouble()))
              .toList())
          .toList();
      setState(() {
        roadSegments = segments;
        selectedRoad = result['nome'];
        roadMessage =
            '$selectedRoad · traçado em cinza: condição desconhecida. Fluxo TomTom apenas nos trechos consultados.';
        roadLoading = false;
      });
      map.fitCamera(CameraFit.bounds(
          bounds: LatLngBounds.fromPoints(segments.expand((s) => s).toList()),
          padding: EdgeInsets.all(24),
          maxZoom: 16));
    } catch (_) {
      if (mounted && generation == roadRequest) {
        setState(() {
          roadMessage =
              'Não foi possível obter o traçado. Confira o nome e tente novamente.';
          roadLoading = false;
        });
      }
    }
  }

  void reload() {
    setState(() => data =
        widget.api.send('/api/transito?regiao=${Uri.encodeComponent(region)}'));
  }

  @override
  void initState() {
    super.initState();
    if (widget.initialRoad == 'Avenida Ana Costa' ||
        widget.initialRoad == 'Avenida Conselheiro Nébias') {
      roadInput.text = widget.initialRoad!;
      region = widget.initialRoad == 'Avenida Ana Costa'
          ? 'Av. Ana Costa'
          : 'Av. Conselheiro Nébias';
      center = region == 'Av. Ana Costa'
          ? LatLng(-23.958, -46.333)
          : LatLng(-23.955, -46.326);
    }
    data =
        widget.api.send('/api/transito?regiao=${Uri.encodeComponent(region)}');
    if (widget.initialRoad != null) {
      data.then((_) async {
        await WidgetsBinding.instance.endOfFrame;
        if (mounted) await selectRoad();
      }).catchError((Object _) {});
    }
    WidgetsBinding.instance.addObserver(this);
    timer = Timer.periodic(Duration(seconds: 5), (_) {
      if (!active || ModalRoute.of(context)?.isCurrent != true) return;
      ticks++;
      if (ticks % 12 == 0) {
        reload();
      } else {
        setState(() {});
      }
    });
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    active = state == AppLifecycleState.resumed;
    if (active) reload();
  }

  @override
  void dispose() {
    roadInput.dispose();
    map.dispose();
    timer?.cancel();
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
      appBar: AppBar(title: Text('Trânsito em Tempo Real'), actions: [
        IconButton(
            tooltip: 'Atualizar',
            icon: Icon(Icons.refresh),
            onPressed: () => setState(() => data = widget.api
                .send('/api/transito?regiao=${Uri.encodeComponent(region)}')))
      ]),
      body: FutureBuilder<dynamic>(
          future: data,
          builder: (context, s) {
            if (s.connectionState == ConnectionState.waiting) {
              return Center(child: CircularProgressIndicator());
            }
            final regions = s.data?['regioes'] as List? ?? [];
            final valid =
                DateTime.tryParse(s.data?['validoAte']?.toString() ?? '');
            final fresh = !s.hasError &&
                valid != null &&
                DateTime.now().toUtc().isBefore(valid);
            return ListView(padding: EdgeInsets.all(24), children: [
              Text('Mobilidade urbana',
                  style: TextStyle(fontSize: 24, fontWeight: FontWeight.w800)),
              SizedBox(height: 16),
              TextField(
                  controller: roadInput,
                  maxLength: 100,
                  decoration: InputDecoration(labelText: 'Via em Santos'),
                  onSubmitted: (_) => selectRoad()),
              Row(children: [
                Expanded(
                    child: OutlinedButton(
                        onPressed: roadLoading ? null : selectRoad,
                        child: Text(
                            roadLoading ? 'Carregando…' : 'Destacar via'))),
                TextButton(
                    onPressed: () {
                      roadRequest++;
                      setState(() {
                        roadSegments = [];
                        selectedRoad = null;
                        roadMessage = null;
                        roadLoading = false;
                      });
                    },
                    child: Text('Limpar seleção'))
              ]),
              if (roadMessage != null) Text(roadMessage!),
              if (regions.isNotEmpty)
                DropdownButtonFormField<String>(
                    initialValue: region,
                    decoration: InputDecoration(labelText: 'Região'),
                    items: regions
                        .map((r) => DropdownMenuItem(
                            value: r['nome'].toString(),
                            child: Text(r['nome'].toString())))
                        .toList(),
                    onChanged: (v) {
                      if (v == null) return;
                      final r = regions.firstWhere((r) => r['nome'] == v);
                      setState(() {
                        region = v;
                        center = LatLng((r['latitude'] as num).toDouble(),
                            (r['longitude'] as num).toDouble());
                        data = widget.api.send(
                            '/api/transito?regiao=${Uri.encodeComponent(v)}');
                      });
                      if (v.startsWith('Av. ')) {
                        roadInput.text = v.replaceFirst('Av. ', 'Avenida ');
                        data.then((_) async {
                          await WidgetsBinding.instance.endOfFrame;
                          if (mounted && region == v) await selectRoad();
                        }).catchError((Object _) {});
                      } else {
                        roadRequest++;
                        setState(() {
                          roadSegments = [];
                          selectedRoad = null;
                          roadMessage = null;
                        });
                      }
                    }),
              SizedBox(height: 16),
              Text(s.hasError
                  ? 'Não foi possível consultar o serviço.'
                  : (valid != null && !fresh
                          ? 'Dados desatualizados. Atualize para consultar as condições atuais.'
                          : s.data?['mensagem'] ??
                              'Dados de trânsito indisponíveis')
                      .toString()),
              Text(
                  'Última consulta: ${s.data?['ultimaAtualizacao'] == null ? 'indisponível' : dateTimeLabel(s.data['ultimaAtualizacao'])}'),
              SizedBox(height: 16),
              SizedBox(
                  height: 360,
                  child: FlutterMap(
                      mapController: map,
                      options:
                          MapOptions(initialCenter: center, initialZoom: 13),
                      children: [
                        TileLayer(
                            urlTemplate:
                                'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                            userAgentPackageName:
                                'br.edu.santosconecta.mobile'),
                        if (!region.startsWith('Av. ') && selectedRoad == null)
                          PolygonLayer(polygons: [
                            Polygon(
                                points: [
                                  LatLng(center.latitude - .012,
                                      center.longitude - .015),
                                  LatLng(center.latitude - .012,
                                      center.longitude + .015),
                                  LatLng(center.latitude + .012,
                                      center.longitude + .015),
                                  LatLng(center.latitude + .012,
                                      center.longitude - .015),
                                ],
                                color: Color(0x10475569),
                                borderColor: Colors.blueGrey,
                                borderStrokeWidth: 2)
                          ]),
                        PolylineLayer(
                            polylines: roadSegments
                                .map((p) => Polyline(
                                    points: p,
                                    color: Colors.blueGrey,
                                    strokeWidth: 10))
                                .toList()),
                        if (fresh && s.data?['disponivel'] == true)
                          PolylineLayer(
                              polylines:
                                  (s.data?['trechos'] as List? ?? []).map((t) {
                            final ratio = (t['velocidadeLivre'] as num) > 0
                                ? (t['velocidadeAtual'] as num) /
                                    (t['velocidadeLivre'] as num)
                                : 0;
                            return Polyline(
                                points: (t['coordenadas'] as List)
                                    .map((p) => LatLng(
                                        (p['latitude'] as num).toDouble(),
                                        (p['longitude'] as num).toDouble()))
                                    .toList(),
                                color: t['interditado'] == true ||
                                        (t['velocidadeLivre'] as num) <= 0
                                    ? Colors.grey
                                    : ratio >= .75
                                        ? Colors.green
                                        : ratio >= .35
                                            ? Colors.amber
                                            : Colors.red,
                                strokeWidth: 7);
                          }).toList()),
                        if (fresh)
                          MarkerLayer(
                              markers: (s.data?['incidentes'] as List? ?? [])
                                  .where((n) =>
                                      (n['coordenadas'] as List).isNotEmpty)
                                  .map((n) {
                            final p = n['coordenadas'][0];
                            return Marker(
                                point: LatLng((p['latitude'] as num).toDouble(),
                                    (p['longitude'] as num).toDouble()),
                                width: 36,
                                height: 36,
                                child: Tooltip(
                                    message: n['descricao'].toString(),
                                    child: Icon(
                                        n['interditado'] == true
                                            ? Icons.block
                                            : Icons.warning_amber_rounded,
                                        color: n['interditado'] == true
                                            ? Colors.grey
                                            : Colors.orange)));
                          }).toList()),
                        RichAttributionWidget(attributions: [
                          TextSourceAttribution(
                              'OpenStreetMap contributors · trânsito © TomTom quando disponível')
                        ])
                      ])),
              SizedBox(height: 16),
              if (!region.startsWith('Av. ') && selectedRoad == null)
                Text(
                    '$region: destaque do recorte de consulta de incidentes; não representa limites oficiais nem uma condição de trânsito para toda a área.'),
              if (fresh)
                ...(s.data?['trechos'] as List? ?? []).map((t) => ListTile(
                    title: Text(t['regiao'].toString()),
                    subtitle: Text(
                        'Velocidade média: ${t['velocidadeAtual']} km/h · Fluxo livre: ${t['velocidadeLivre']} km/h${t['interditado'] == true ? ' · Interditado' : ''}'))),
              Text('Incidentes e interdições',
                  style: TextStyle(fontWeight: FontWeight.w700)),
              Text(fresh
                  ? (s.data?['mensagemIncidentes'] ??
                          'Incidentes indisponíveis.')
                      .toString()
                  : 'Incidentes atuais indisponíveis.'),
              if (fresh)
                ...(s.data?['incidentes'] as List? ?? []).map((n) => Card(
                    child: ListTile(
                        leading: Icon(Icons.warning_amber_rounded),
                        title: Text(n['descricao'].toString()),
                        subtitle: Text('${n['de']} → ${n['ate']}')))),
              Text(
                  'Legenda: verde — fluxo livre; amarelo — lentidão; vermelho — congestionamento; cinza — condição desconhecida ou interdição indicada no detalhe; laranja — incidente. Sem uma fonte real conectada, nenhuma condição atual é afirmada.'),
            ]);
          }));
}
