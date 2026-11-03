import 'package:flutter/material.dart';
import '../services/api_client.dart';
import '../services/weather_push.dart';

class WeatherAlertsScreen extends StatefulWidget {
  final ApiClient api;
  const WeatherAlertsScreen({super.key, required this.api});
  @override
  State<WeatherAlertsScreen> createState() => _WeatherAlertsScreenState();
}

class _WeatherAlertsScreenState extends State<WeatherAlertsScreen> {
  late Future<dynamic> data;
  bool history = false;
  bool busy = false;
  int page = 1;
  String? message;
  @override
  void initState() {
    super.initState();
    load();
  }

  void load() {
    data = widget.api.send(history
        ? '/api/clima/alertas/historico?pagina=$page'
        : '/api/clima/alertas');
  }

  Future<void> preference(bool enabled) async {
    setState(() => busy = true);
    try {
      await WeatherPush.configure(widget.api, enabled);
      if (mounted) {
        setState(() => message = enabled
            ? 'Notificações autorizadas neste dispositivo.'
            : 'Notificações meteorológicas desativadas.');
      }
    } catch (_) {
      if (mounted) {
        setState(() => message =
            'Não foi possível alterar a preferência. Firebase configurado e permissão do sistema são necessários.');
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
      appBar: AppBar(title: Text('Central de Alertas')),
      body: ListView(padding: EdgeInsets.all(20), children: [
        Text(
            'Avisos preventivos automáticos são gerados pelo Santos Conector. Não são alertas oficiais da Defesa Civil.',
            style: TextStyle(fontWeight: FontWeight.bold)),
        SizedBox(height: 12),
        Wrap(spacing: 8, children: [
          OutlinedButton(
              onPressed: busy ? null : () => preference(true),
              child: Text('Ativar notificações')),
          TextButton(
              onPressed: busy ? null : () => preference(false),
              child: Text('Desativar'))
        ]),
        if (message != null) Text(message!),
        SwitchListTile(
            title: Text('Consultar histórico'),
            value: history,
            onChanged: (v) => setState(() {
                  history = v;
                  page = 1;
                  load();
                })),
        TextButton(
            onPressed: () => setState(load), child: Text('Atualizar avisos')),
        FutureBuilder<dynamic>(
            future: data,
            builder: (context, s) {
              if (s.connectionState == ConnectionState.waiting) {
                return Center(child: CircularProgressIndicator());
              }
              if (s.hasError) {
                return Text(
                    'Não foi possível consultar os avisos. Tente atualizar.');
              }
              final items = s.data as List? ?? [];
              if (items.isEmpty) {
                return Text(
                    'Nenhum aviso disponível nesta consulta. Isso não garante ausência de riscos.');
              }
              return Column(
                  children: items
                      .map((a) => Card(
                          child: ListTile(
                              leading: Icon(Icons.warning_amber,
                                  color: Colors.orange),
                              title: Text(a['titulo']),
                              subtitle: Text(
                                  '${a['origem'] == 'Automatico' ? 'Aviso preventivo automático' : 'Alerta oficial'} · ${a['severidade']}\n${a['fonte']}'),
                              onTap: () => showDialog<void>(
                                  context: context,
                                  builder: (context) => AlertDialog(
                                          title: Text(a['titulo']),
                                          content: SingleChildScrollView(
                                              child: Column(
                                                  crossAxisAlignment:
                                                      CrossAxisAlignment.start,
                                                  mainAxisSize: MainAxisSize.min,
                                                  children: [
                                                Text(a['descricao']),
                                                Text(
                                                    '\nFonte: ${a['fonte']}\nRegião: ${a['regiao']}\nPublicado: ${a['publicadoEm']} UTC\nValidade: ${a['inicio']} a ${a['fim']} UTC\nEstado: ${a['ativo'] == true ? 'Ativo na última atualização' : 'Encerrado'}'),
                                                SelectableText(a['referencia'])
                                              ])),
                                          actions: [
                                            TextButton(
                                                onPressed: () =>
                                                    Navigator.pop(context),
                                                child: Text('Fechar'))
                                          ])))))
                      .toList());
            }),
        if (history)
          Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
            TextButton(
                onPressed: page > 1
                    ? () => setState(() {
                          page--;
                          load();
                        })
                    : null,
                child: Text('Anterior')),
            Text('Página $page'),
            TextButton(
                onPressed: () => setState(() {
                      page++;
                      load();
                    }),
                child: Text('Próxima'))
          ]),
      ]));
}
