import 'dart:async';
import 'package:flutter/material.dart';
import '../services/api_client.dart';
import 'identity.dart';

class OccurrenceMessages extends StatefulWidget {
  final ApiClient api;
  final int id;
  final int? messageId;
  const OccurrenceMessages(
      {super.key, required this.api, required this.id, this.messageId});
  @override
  State<OccurrenceMessages> createState() => _OccurrenceMessagesState();
}

class _OccurrenceMessagesState extends State<OccurrenceMessages>
    with WidgetsBindingObserver {
  final text = TextEditingController();
  List<dynamic> messages = [];
  Timer? timer;
  String? error;
  bool loading = true, busy = false, fetching = false, active = true;
  bool opened = false;
  final highlighted = GlobalKey();
  String get path => '/api/ocorrencias/${widget.id}/mensagens';
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    load();
    timer = Timer.periodic(Duration(seconds: 15), (_) {
      if (active && ModalRoute.of(context)?.isCurrent == true) load();
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
    text.dispose();
    super.dispose();
  }

  Future<void> load() async {
    if (fetching) return;
    fetching = true;
    try {
      final result = await widget.api.send(path) as List;
      if (!mounted) return;
      setState(() {
        messages = result;
        error = null;
        loading = false;
      });
      if (!opened &&
          widget.messageId != null &&
          result.any((m) => m['idMensagem'] == widget.messageId)) {
        opened = true;
        WidgetsBinding.instance.addPostFrameCallback((_) async {
          if (!mounted) return;
          final target = highlighted.currentContext;
          if (target != null) {
            await Scrollable.ensureVisible(target,
                duration: Duration(milliseconds: 250), alignment: .2);
          }
          if (!mounted) return;
          try {
            await widget.api.send('/api/notificacoes/${widget.messageId}/lida',
                method: 'PUT');
            await load();
          } catch (e) {
            if (mounted) setState(() => error = e.toString());
          }
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          error = e.toString();
          loading = false;
        });
      }
    } finally {
      fetching = false;
    }
  }

  Future<void> send() async {
    setState(() => busy = true);
    try {
      await widget.api
          .send(path, method: 'POST', body: {'conteudo': text.text.trim()});
      text.clear();
      await load();
    } catch (e) {
      if (mounted) setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) =>
      Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Text('Mensagens da equipe',
            style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700)),
        SizedBox(height: 12),
        if (loading) LinearProgressIndicator(),
        if (error != null)
          Text(error!,
              style: TextStyle(color: Theme.of(context).colorScheme.error)),
        if (!loading && messages.isEmpty)
          Text('Nenhuma mensagem. Você pode falar com a equipe por aqui.'),
        if (messages.any(
            (m) => m['tipoRemetente'] == 'Equipe' && m['dataLeitura'] == null))
          TextButton.icon(
              icon: Icon(Icons.mark_email_read_outlined),
              label: Text('Novas mensagens · marcar como lidas'),
              onPressed: () async {
                try {
                  await widget.api.send('$path/lidas', method: 'POST');
                  await load();
                } catch (e) {
                  if (mounted) setState(() => error = e.toString());
                }
              }),
        ...messages.map((m) => Container(
            key: m['idMensagem'] == widget.messageId ? highlighted : null,
            margin: EdgeInsets.symmetric(vertical: 6),
            padding: EdgeInsets.all(16),
            decoration: BoxDecoration(
                color: m['tipoRemetente'] == 'Equipe'
                    ? Theme.of(context).colorScheme.secondaryContainer
                    : Theme.of(context).colorScheme.surface,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(
                    color: m['idMensagem'] == widget.messageId
                        ? Theme.of(context).colorScheme.primary
                        : Theme.of(context).colorScheme.outlineVariant,
                    width: m['idMensagem'] == widget.messageId ? 3 : 1)),
            child:
                Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(
                  '${m['nomeRemetente']} · ${m['tipoRemetente'] == 'Equipe' ? 'Equipe' : 'Você'}',
                  style: TextStyle(fontWeight: FontWeight.w700)),
              SizedBox(height: 8),
              Text(m['conteudo'].toString()),
              SizedBox(height: 8),
              Text(dateTimeLabel(m['dataEnvio']),
                  style: TextStyle(fontSize: 12))
            ]))),
        SizedBox(height: 16),
        TextField(
            controller: text,
            maxLength: 4000,
            minLines: 2,
            maxLines: 5,
            decoration: InputDecoration(labelText: 'Responder à equipe')),
        FilledButton(
            onPressed: busy
                ? null
                : () {
                    if (text.text.trim().isNotEmpty) send();
                  },
            child: Text(busy ? 'Enviando…' : 'Enviar mensagem')),
      ]);
}
