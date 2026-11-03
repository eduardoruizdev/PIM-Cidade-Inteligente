import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../services/api_client.dart';
import '../services/ocorrencia_service.dart';
import '../widgets/identity.dart';
import '../widgets/occurrence_messages.dart';

class OccurrenceDetailScreen extends StatefulWidget {
  final ApiClient api;
  final int id;
  final int? messageId;
  const OccurrenceDetailScreen({
    super.key,
    required this.api,
    required this.id,
    this.messageId,
  });
  @override
  State<OccurrenceDetailScreen> createState() => _OccurrenceDetailScreenState();
}

class _OccurrenceDetailScreenState extends State<OccurrenceDetailScreen> {
  late Future<Map<String, dynamic>> data;
  @override
  void initState() {
    super.initState();
    data = OcorrenciaService(widget.api).detalhes(widget.id);
  }

  Future<void> refresh() async {
    final next = OcorrenciaService(widget.api).detalhes(widget.id);
    setState(() => data = next);
    await next;
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(
          title: Text('Detalhes da reclamação'),
          actions: [
            IconButton(
              onPressed: () => refresh(),
              tooltip: 'Atualizar',
              icon: Icon(Icons.refresh),
            ),
          ],
        ),
        body: FutureBuilder<Map<String, dynamic>>(
          future: data,
          builder: (context, s) {
            if (s.connectionState == ConnectionState.waiting) {
              return Center(child: CircularProgressIndicator());
            }
            if (s.hasError) {
              return Center(
                  child: Padding(
                      padding: EdgeInsets.all(24),
                      child: Column(mainAxisSize: MainAxisSize.min, children: [
                        Text(s.error.toString(), textAlign: TextAlign.center),
                        SizedBox(height: 16),
                        FilledButton(
                            onPressed: refresh, child: Text('Tentar novamente'))
                      ])));
            }
            if (!s.hasData) return SizedBox.shrink();
            final o = Map<String, dynamic>.from(s.data!['ocorrencia']);
            final history = (o['historico'] as List?) ?? [];
            final responses = (s.data!['respostas'] as List?) ?? [];
            final location = LatLng(
              (o['latitude'] as num).toDouble(),
              (o['longitude'] as num).toDouble(),
            );
            // Etapas só são marcadas se há evento correspondente ou se são o estado atual.
            final events =
                history.map((h) => h['descricao'].toString()).toList();
            final statuses = [
              'Aberta',
              'Em análise',
              'Em andamento',
              'Resolvida'
            ];
            return RefreshIndicator(
              onRefresh: refresh,
              child: ListView(
                padding: EdgeInsets.all(24),
                children: [
                  if (widget.messageId != null)
                    OccurrenceMessages(
                        api: widget.api,
                        id: widget.id,
                        messageId: widget.messageId),
                  Text(
                    o['categoria'].toString(),
                    style: TextStyle(
                      fontSize: 25,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  SizedBox(height: 8),
                  Text(
                    'Protocolo ${o['protocolo']}',
                    style: TextStyle(fontSize: 12),
                  ),
                  StatusChip(o['status'].toString()),
                  Divider(height: 32),
                  Text(
                    'Acompanhamento',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
                  ),
                  SizedBox(height: 12),
                  ...statuses.map((status) {
                    final verified = status == 'Aberta' ||
                        o['status'] == status ||
                        events.any((e) => e == 'Status alterado para $status.');
                    return Padding(
                      padding: EdgeInsets.symmetric(vertical: 8),
                      child: Row(
                        children: [
                          Icon(
                            verified
                                ? Icons.check_circle
                                : Icons.radio_button_unchecked,
                            color: verified
                                ? Theme.of(context).colorScheme.primary
                                : Colors.grey,
                            size: 22,
                          ),
                          SizedBox(width: 12),
                          Text(
                            status == 'Aberta' ? 'Registrada' : status,
                            style: TextStyle(
                              fontWeight: o['status'] == status
                                  ? FontWeight.bold
                                  : FontWeight.normal,
                            ),
                          ),
                          if (o['status'] == status)
                            Text('  · atual', style: TextStyle(fontSize: 11)),
                        ],
                      ),
                    );
                  }),
                  if (o['status'] == 'Cancelada')
                    Text('Reclamação cancelada. Consulte o histórico.'),
                  Divider(height: 32),
                  Text(
                    o['descricao'].toString(),
                    style: TextStyle(fontSize: 15, height: 1.7),
                  ),
                  SizedBox(height: 20),
                  Text(
                    '${o['endereco']}\n${o['bairro']} · ${dateLabel(o['dataAbertura'])}',
                    style: TextStyle(height: 1.7),
                  ),
                  SizedBox(height: 16),
                  if (o['localizacaoConfirmada'] == false)
                    Text(
                        'Localização no mapa não confirmada. Endereço informado pelo cidadão.'),
                  if (o['localizacaoConfirmada'] != false)
                    SizedBox(
                      height: 220,
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(8),
                        child: FlutterMap(
                          options: MapOptions(
                            initialCenter: location,
                            initialZoom: 15,
                          ),
                          children: [
                            TileLayer(
                              urlTemplate:
                                  'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                              userAgentPackageName:
                                  'br.edu.santosconecta.mobile',
                            ),
                            MarkerLayer(
                              markers: [
                                Marker(
                                  point: location,
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
                    ),
                  SizedBox(height: 28),
                  Text(
                    'Histórico de atualizações',
                    style: TextStyle(fontWeight: FontWeight.w700, fontSize: 18),
                  ),
                  ...history.map(
                    (h) => ListTile(
                      contentPadding: EdgeInsets.zero,
                      leading: Icon(Icons.history),
                      title: Text(h['descricao'].toString()),
                      subtitle: Text(dateTimeLabel(h['dataAlteracao'])),
                    ),
                  ),
                  SizedBox(height: 24),
                  Text(
                    'Respostas da equipe',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
                  ),
                  if (responses.isEmpty)
                    Padding(
                      padding: EdgeInsets.symmetric(vertical: 16),
                      child: Text('Nenhuma resposta enviada até o momento.'),
                    ),
                  if (widget.messageId == null)
                    OccurrenceMessages(api: widget.api, id: widget.id),
                  ...responses.map(
                    (r) => Card(
                      elevation: 0,
                      child: Padding(
                        padding: EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              r['assunto'].toString(),
                              style: TextStyle(fontWeight: FontWeight.bold),
                            ),
                            Text(dateTimeLabel(r['dataEnvio'])),
                            SizedBox(height: 12),
                            Text(
                              r['mensagem'].toString(),
                              style: TextStyle(height: 1.6),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            );
          },
        ),
      );
}
