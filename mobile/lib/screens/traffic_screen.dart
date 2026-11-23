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
  void reload() {
    setState(() => data =
        widget.api.send('/api/transito?regiao=${Uri.encodeComponent(region)}'));
  }

  @override
  void initState() {
    super.initState();
    if (widget.initialRoad == 'Avenida Ana Costa' ||
        widget.initialRoad == 'Avenida Conselheiro Nébias') {
      region = widget.initialRoad == 'Avenida Ana Costa'
          ? 'Av. Ana Costa'
          : 'Av. Conselheiro Nébias';
      center = region == 'Av. Ana Costa'
          ? LatLng(-23.966537, -46.332837)
          : LatLng(-23.955, -46.326);
    }
    data =
        widget.api.send('/api/transito?regiao=${Uri.encodeComponent(region)}');
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
                      map.move(center, 13);
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
              Wrap(spacing: 8, runSpacing: 8, children: [
                _summary('Tranquilos', s.data?['trechosTranquilos'], Colors.green, fresh),
                _summary('Moderados', s.data?['trechosModerados'], Colors.amber.shade800, fresh),
                _summary('Congestionados', s.data?['trechosCongestionados'], Colors.red, fresh),
                _summary('Sem informacao', s.data?['trechosSemInformacao'], Colors.blueGrey, fresh),
              ]),
              Text(fresh
                  ? 'Situacao geral das amostras: ${s.data?['situacaoGeral']}. Os numeros contam somente trechos retornados, nao todas as vias da regiao.'
                  : 'Indicadores indisponiveis: nao ha amostras atuais.'),
              SizedBox(height: 12),
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
                        if (s.data?['camadaMapaDisponivel'] == true)
                          TileLayer(
                              key: ValueKey('tomtom-flow-$region-${ticks ~/ 12}'),
                              urlTemplate: widget.api.trafficTileUrl,
                              tileProvider: NetworkTileProvider(
                                  headers: widget.api.trafficTileHeaders),
                              maxNativeZoom: 18,
                              tileDisplay: TileDisplay.fadeIn(duration: Duration(milliseconds: 180)),
                              userAgentPackageName:
                                  'br.edu.santosconecta.mobile'),
                        if (!region.startsWith('Av. '))
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
              if (!region.startsWith('Av. '))
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

  Widget _summary(String label, dynamic count, Color color, bool fresh) => Chip(
        avatar: CircleAvatar(backgroundColor: color, radius: 7),
        label: Text('$label · ${fresh ? count ?? 0 : '—'}'),
      );
}
