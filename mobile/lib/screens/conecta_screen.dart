import 'package:flutter/material.dart';
import '../services/api_client.dart';

class ConectaScreen extends StatefulWidget {
  final ApiClient api;
  const ConectaScreen({super.key, required this.api});
  @override
  State<ConectaScreen> createState() => _ConectaScreenState();
}

class _ConectaScreenState extends State<ConectaScreen> {
  final input = TextEditingController();
  final scroll = ScrollController();
  List<dynamic> messages = [];
  bool loading = true, busy = false;
  String? error;
  String retention = 'Histórico temporário, isolado por conta.';
  @override
  void initState() {
    super.initState();
    load();
  }

  @override
  void dispose() {
    input.dispose();
    scroll.dispose();
    super.dispose();
  }

  void end() => WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted && scroll.hasClients) {
          scroll.animateTo(scroll.position.maxScrollExtent,
              duration: Duration(milliseconds: 200), curve: Curves.easeOut);
        }
      });
  Future<void> load() async {
    try {
      final data = await widget.api.send('/api/conecta/historico');
      if (!mounted) return;
      setState(() {
        messages = data['mensagens'] as List;
        retention = data['retencao'].toString();
        error = null;
      });
      end();
    } catch (e) {
      if (mounted) setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> send([String? suggestion]) async {
    final text = (suggestion ?? input.text).trim();
    if (text.isEmpty || busy || loading) return;
    setState(() {
      busy = true;
      error = null;
      messages = [
        ...messages,
        {'papel': 'user', 'texto': text, 'acoes': []}
      ];
    });
    input.clear();
    end();
    try {
      final answer = await widget.api.send('/api/conecta/mensagens',
          method: 'POST', body: {'mensagem': text});
      if (!mounted) return;
      setState(() => messages = [...messages, answer]);
      end();
    } catch (e) {
      if (mounted) {
        setState(() {
          error =
              '${e.toString()} Sua pergunta não foi confirmada. Você pode tentar novamente.';
          input.text = text;
        });
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> clear() async {
    if (busy) return;
    try {
      await widget.api.send('/api/conecta/historico', method: 'DELETE');
      if (mounted) {
        setState(() {
          messages = [];
          error = null;
        });
      }
    } catch (e) {
      if (mounted) setState(() => error = e.toString());
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
      appBar: AppBar(title: Text('Conecta'), actions: [
        IconButton(
            tooltip: 'Limpar histórico',
            onPressed: busy ? null : clear,
            icon: Icon(Icons.delete_outline))
      ]),
      body: SafeArea(
          child: Column(children: [
        Padding(
            padding: EdgeInsets.symmetric(horizontal: 20, vertical: 8),
            child:
                Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text('Seu assistente do Santos Conector',
                  style: TextStyle(fontWeight: FontWeight.bold)),
              Text(retention, style: TextStyle(fontSize: 11)),
              Text('Não envie senhas, documentos ou outros dados pessoais.',
                  style: TextStyle(fontSize: 11)),
            ])),
        if (loading) LinearProgressIndicator(),
        Expanded(
            child: ListView(
                controller: scroll,
                padding: EdgeInsets.all(16),
                children: [
              if (messages.isEmpty && !loading)
                Padding(
                    padding: EdgeInsets.all(12),
                    child: Text(
                        'Olá! Sou o Conecta. Como posso ajudar você hoje?')),
              ...messages.map((m) {
                final user = m['papel'] == 'user';
                return Align(
                    alignment:
                        user ? Alignment.centerRight : Alignment.centerLeft,
                    child: Container(
                        constraints: BoxConstraints(maxWidth: 600),
                        margin: EdgeInsets.only(bottom: 12),
                        padding: EdgeInsets.all(16),
                        decoration: BoxDecoration(
                            color: user
                                ? Theme.of(context).colorScheme.primary
                                : Theme.of(context).colorScheme.surface,
                            borderRadius: BorderRadius.circular(16)),
                        child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(user ? 'Você' : 'Conecta',
                                  style: TextStyle(
                                      fontWeight: FontWeight.bold,
                                      color: user
                                          ? Theme.of(context)
                                              .colorScheme
                                              .surface
                                          : Theme.of(context)
                                              .colorScheme
                                              .primary)),
                              SizedBox(height: 6),
                              SelectableText(m['texto'].toString(),
                                  style: TextStyle(
                                      color: user
                                          ? Theme.of(context)
                                              .colorScheme
                                              .surface
                                          : Theme.of(context)
                                              .colorScheme
                                              .onSurface,
                                      height: 1.5)),
                              if (!user)
                                Padding(
                                    padding: EdgeInsets.only(top: 8),
                                    child: Text(
                                        m['modo'] == 'inteligente'
                                            ? 'Interpretação por IA · dados consultados pelo sistema'
                                            : 'Modo orientativo · IA indisponível ou não utilizada',
                                        style: TextStyle(fontSize: 10))),
                              Wrap(
                                  spacing: 8,
                                  children: (m['acoes'] as List? ?? [])
                                      .map((a) => TextButton(
                                          onPressed: busy
                                              ? null
                                              : () => Navigator.pop(context,
                                                  Map<String, dynamic>.from(a)),
                                          child: Text(a['rotulo'].toString())))
                                      .toList()),
                            ])));
              }),
              if (busy)
                ListTile(
                    leading: SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(strokeWidth: 2)),
                    title: Text('Conecta está consultando…')),
            ])),
        if (error != null)
          Padding(
              padding: EdgeInsets.symmetric(horizontal: 16),
              child: Row(children: [
                Expanded(
                    child: Text(error!,
                        style: TextStyle(
                            color: Theme.of(context).colorScheme.error))),
                if (!busy)
                  TextButton(onPressed: load, child: Text('Recarregar'))
              ])),
        if (messages.isEmpty)
          SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              padding: EdgeInsets.symmetric(horizontal: 16),
              child: Row(
                  children: [
                'Quero reclamar de um buraco',
                'Minhas reclamações foram respondidas?',
                'Vai chover amanhã?',
                'Trânsito na Ana Costa'
              ]
                      .map((q) => Padding(
                          padding: EdgeInsets.only(right: 8),
                          child: ActionChip(
                              label: Text(q),
                              onPressed:
                                  loading || busy ? null : () => send(q))))
                      .toList())),
        Padding(
            padding: EdgeInsets.all(12),
            child: Row(crossAxisAlignment: CrossAxisAlignment.end, children: [
              Expanded(
                  child: TextField(
                      controller: input,
                      maxLength: 1000,
                      minLines: 1,
                      maxLines: 4,
                      decoration:
                          InputDecoration(labelText: 'Pergunte ao Conecta'),
                      onSubmitted: (_) => send())),
              SizedBox(width: 8),
              IconButton.filled(
                  tooltip: 'Enviar mensagem',
                  onPressed: busy || loading ? null : () => send(),
                  icon: Icon(Icons.send_rounded))
            ])),
      ])));
}
