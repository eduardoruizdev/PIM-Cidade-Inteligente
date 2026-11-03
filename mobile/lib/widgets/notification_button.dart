import 'dart:async';
import 'package:flutter/material.dart';
import '../services/api_client.dart';
import '../screens/occurrence_detail_screen.dart';
import 'identity.dart';

class NotificationButton extends StatefulWidget {
  final ApiClient api;
  const NotificationButton({super.key, required this.api});
  @override
  State<NotificationButton> createState() => _NotificationButtonState();
}

class _NotificationButtonState extends State<NotificationButton>
    with WidgetsBindingObserver {
  final items = ValueNotifier<List<dynamic>>([]);
  final error = ValueNotifier<String?>(null);
  Timer? timer;
  bool active = true, fetching = false;
  int count = 0;
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    load();
    timer = Timer.periodic(Duration(seconds: 30), (_) {
      if (active) load();
    });
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    active = state == AppLifecycleState.resumed;
    if (active) load();
  }

  @override
  void dispose() {
    timer?.cancel();
    WidgetsBinding.instance.removeObserver(this);
    items.dispose();
    error.dispose();
    super.dispose();
  }

  Future<void> load() async {
    if (fetching) return;
    fetching = true;
    try {
      final results = await Future.wait([
        widget.api.send('/api/notificacoes'),
        widget.api.send('/api/notificacoes/nao-lidas/quantidade'),
      ]);
      if (mounted) {
        items.value = results[0] as List;
        error.value = null;
        setState(() => count = (results[1]['quantidade'] as num).toInt());
      }
    } catch (e) {
      if (mounted) error.value = e.toString();
    } finally {
      fetching = false;
    }
  }

  Future<void> open() async {
    await load();
    if (!mounted) return;
    await Navigator.of(context).push(MaterialPageRoute(
        builder: (context) => Scaffold(
              appBar: AppBar(title: Text('Notificações'), actions: [
                IconButton(
                    tooltip: 'Atualizar',
                    icon: Icon(Icons.refresh),
                    onPressed: load)
              ]),
              body: ValueListenableBuilder<List<dynamic>>(
                  valueListenable: items,
                  builder: (context, list, _) => RefreshIndicator(
                        onRefresh: load,
                        child: ListView(
                            padding: EdgeInsets.all(20),
                            physics: AlwaysScrollableScrollPhysics(),
                            children: [
                              Text('Respostas da equipe',
                                  style: TextStyle(
                                      fontSize: 22,
                                      fontWeight: FontWeight.w700)),
                              Text(
                                  'Avisos dentro do aplicativo. Atualização a cada 30 segundos durante o uso.'),
                              ValueListenableBuilder<String?>(
                                  valueListenable: error,
                                  builder: (_, message, _) => message == null
                                      ? SizedBox.shrink()
                                      : Text(message,
                                          style: TextStyle(
                                              color: Theme.of(context)
                                                  .colorScheme
                                                  .error))),
                              if (list.isEmpty)
                                Padding(
                                    padding: EdgeInsets.all(32),
                                    child: Text(
                                        'Nenhuma resposta recebida. As mensagens da equipe aparecerão aqui.')),
                              ...list.map((m) => Card(
                                  color: m['dataLeitura'] == null
                                      ? Theme.of(context)
                                          .colorScheme
                                          .secondaryContainer
                                      : Theme.of(context).colorScheme.surface,
                                  child: ListTile(
                                    leading: Icon(m['dataLeitura'] == null
                                        ? Icons.mark_email_unread_outlined
                                        : Icons.drafts_outlined),
                                    title: Text(
                                        '${m['protocolo']} · ${m['categoria']}'),
                                    subtitle: Column(
                                        crossAxisAlignment:
                                            CrossAxisAlignment.start,
                                        children: [
                                          Text(m['conteudo'].toString(),
                                              maxLines: 3,
                                              overflow: TextOverflow.ellipsis),
                                          Text(
                                              '${dateTimeLabel(m['dataEnvio'])} · ${m['dataLeitura'] == null ? 'Não lida' : 'Lida'}'),
                                        ]),
                                    onTap: () async {
                                      await Navigator.of(context).push(
                                          MaterialPageRoute(
                                              builder: (_) =>
                                                  OccurrenceDetailScreen(
                                                      api: widget.api,
                                                      id: m['idOcorrencia']
                                                          as int,
                                                      messageId: m['idMensagem']
                                                          as int)));
                                      await load();
                                    },
                                  )))
                            ]),
                      )),
            )));
    await load();
  }

  @override
  Widget build(BuildContext context) => IconButton(
        tooltip: 'Notificações: $count mensagens não lidas',
        onPressed: open,
        icon: Badge(
            isLabelVisible: count > 0,
            label: Text(count > 99 ? '99+' : '$count'),
            child: Icon(Icons.notifications_outlined)),
      );
}
