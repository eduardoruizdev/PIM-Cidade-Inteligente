import 'package:flutter/material.dart';
import 'dart:async';
import '../services/weather_push.dart';

import '../services/api_client.dart';
import '../services/ocorrencia_service.dart';
import '../services/perfil_service.dart';
import '../widgets/identity.dart';
import '../widgets/notification_button.dart';
import '../widgets/weather_alert_banner.dart';
import 'new_occurrence_screen.dart';
import 'traffic_screen.dart';
import 'weather_screen.dart';
import 'weather_alerts_screen.dart';
import 'conecta_screen.dart';
import 'nearby_services_screen.dart';
import 'ferry_screen.dart';
import '../services/appearance.dart';
import 'package:flutter/foundation.dart';
import 'occurrence_detail_screen.dart';

class HomeScreen extends StatefulWidget {
  final ApiClient api;
  final Map<String, dynamic> profile;
  final VoidCallback onLogout;
  final ValueChanged<Map<String, dynamic>> onProfile;
  const HomeScreen({
    super.key,
    required this.api,
    required this.profile,
    required this.onLogout,
    required this.onProfile,
  });
  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  StreamSubscription<String>? pushOpened;
  int tab = 0;
  String filter = '';
  late Future<List<Map<String, dynamic>>> data;
  @override
  void initState() {
    super.initState();
    data = OcorrenciaService(widget.api).minhas();
    pushOpened = WeatherPush.opened.listen((_) {
      if (mounted) {
        Navigator.push(
            context,
            MaterialPageRoute(
                builder: (_) => WeatherAlertsScreen(api: widget.api)));
      }
    });
    WeatherPush.restore(widget.api).catchError((Object _) {});
    if (kIsWeb && Uri.base.queryParameters['acao'] == 'nova') {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) newOccurrence();
      });
    }
  }

  @override
  void dispose() {
    pushOpened?.cancel();
    super.dispose();
  }

  Future<void> refresh() async {
    final next = OcorrenciaService(widget.api).minhas();
    setState(() => data = next);
    await next;
  }

  Future<void> newOccurrence() async {
    final saved = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => NewOccurrenceScreen(api: widget.api)),
    );
    if (saved == true) await refresh();
  }

  Future<void> detail(Map<String, dynamic> o) async {
    await Navigator.of(context).push<void>(
      MaterialPageRoute(
        builder: (_) => OccurrenceDetailScreen(
          api: widget.api,
          id: o['idOcorrencia'] as int,
        ),
      ),
    );
    await refresh();
  }

  Future<void> openConecta() async {
    final action = await Navigator.push<Map<String, dynamic>>(context,
        MaterialPageRoute(builder: (_) => ConectaScreen(api: widget.api)));
    if (!mounted || action == null) return;
    switch (action['id']) {
      case 'NOVA_RECLAMACAO':
        await newOccurrence();
        break;
      case 'MINHAS_RECLAMACOES':
      case 'MENSAGENS':
        setState(() => tab = 1);
        await refresh();
        break;
      case 'DETALHE_RECLAMACAO':
        final id = action['ocorrencia'];
        if (id is int && id > 0) {
          await Navigator.push(
              context,
              MaterialPageRoute(
                  builder: (_) =>
                      OccurrenceDetailScreen(api: widget.api, id: id)));
        }
        break;
      case 'CLIMA':
        await Navigator.push(context,
            MaterialPageRoute(builder: (_) => WeatherScreen(api: widget.api)));
        break;
      case 'ALERTAS':
        await Navigator.push(
            context,
            MaterialPageRoute(
                builder: (_) => WeatherAlertsScreen(api: widget.api)));
        break;
      case 'TRANSITO':
        await Navigator.push(
            context,
            MaterialPageRoute(
                builder: (_) => TrafficScreen(
                    api: widget.api,
                    initialRoad:
                        action['via'] is String ? action['via'] : null)));
        break;
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(
          title: LayoutBuilder(
              builder: (context, constraints) =>
                  MediaQuery.textScalerOf(context).scale(24) > 32 ||
                          constraints.maxWidth < 150
                      ? Semantics(
                          label: 'Santos Conecta',
                          child: Icon(Icons.waves_rounded, size: 36))
                      : Identity()),
          actions: [
            const AppearanceButton(),
            NotificationButton(api: widget.api),
            IconButton(
                tooltip: 'Trânsito em Santos',
                icon: Icon(Icons.traffic_outlined),
                onPressed: () => Navigator.of(context).push(MaterialPageRoute(
                    builder: (_) => TrafficScreen(api: widget.api)))),
            IconButton(
              tooltip: 'Atualizar',
              onPressed: () => refresh(),
              icon: Icon(Icons.refresh),
            ),
          ],
        ),
        bottomNavigationBar: NavigationBar(
          selectedIndex: tab,
          onDestinationSelected: (i) {
            if (i == 4) {
              Navigator.push(
                  context,
                  MaterialPageRoute(
                      builder: (_) => NearbyServicesScreen(api: widget.api)));
              return;
            }
            if (i == 3) {
              openConecta();
            } else {
              setState(() => tab = i);
            }
          },
          destinations: [
            NavigationDestination(
              icon: Icon(Icons.home_outlined),
              selectedIcon: Icon(Icons.home),
              label: 'Início',
            ),
            NavigationDestination(
              icon: Icon(Icons.receipt_long_outlined),
              label: 'Reclamações',
            ),
            NavigationDestination(
              icon: Icon(Icons.person_outline),
              label: 'Perfil',
            ),
            NavigationDestination(
                icon: Icon(Icons.forum_outlined), label: 'Conecta'),
            const NavigationDestination(
                icon: Icon(Icons.location_city_outlined), label: 'Serviços'),
          ],
        ),
        body: tab == 2
            ? ProfileScreen(
                api: widget.api,
                profile: widget.profile,
                onProfile: widget.onProfile,
                onLogout: widget.onLogout,
              )
            : FutureBuilder<List<Map<String, dynamic>>>(
                future: data,
                builder: (context, snapshot) {
                  if (snapshot.connectionState == ConnectionState.waiting) {
                    return Center(child: CircularProgressIndicator());
                  }
                  if (snapshot.hasError) {
                    return Center(
                      child: Padding(
                        padding: EdgeInsets.all(24),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Text(snapshot.error.toString()),
                            SizedBox(height: 16),
                            FilledButton(
                              onPressed: () => refresh(),
                              child: Text('Tentar novamente'),
                            ),
                          ],
                        ),
                      ),
                    );
                  }
                  final all = snapshot.data ?? [];
                  final items = all
                      .where((o) => filter.isEmpty || o['status'] == filter)
                      .toList();
                  return RefreshIndicator(
                    onRefresh: refresh,
                    child: ListView(
                      physics: AlwaysScrollableScrollPhysics(),
                      padding: EdgeInsets.all(24),
                      children: [
                        if (tab == 0) ...[
                          WeatherAlertBanner(api: widget.api),
                          Card(
                            child: ListTile(
                              leading: const Icon(Icons.directions_boat_outlined),
                              title: const Text('Travessia Santos–Guarujá'),
                              subtitle: const Text('Disponibilidade e consulta oficial'),
                              trailing: const Icon(Icons.chevron_right),
                              onTap: () => Navigator.push(context, MaterialPageRoute(
                                builder: (_) => FerryScreen(api: widget.api))),
                            ),
                          ),
                          Card(
                              child: ListTile(
                                  leading: Icon(Icons.cloud_outlined),
                                  title: Text('Clima em Santos'),
                                  subtitle: Text('Condições atuais e previsão'),
                                  trailing: Icon(Icons.chevron_right),
                                  onTap: () => Navigator.push(
                                      context,
                                      MaterialPageRoute(
                                          builder: (_) => WeatherScreen(
                                              api: widget.api))))),
                          Card(
                              child: ListTile(
                                  leading: Icon(Icons.warning_amber),
                                  title: Text('Central de Alertas'),
                                  subtitle: Text('Avisos ativos e histórico'),
                                  onTap: () => Navigator.push(
                                      context,
                                      MaterialPageRoute(
                                          builder: (_) => WeatherAlertsScreen(
                                              api: widget.api))))),
                          Text(
                            'Olá, ${widget.profile['nome'].toString().split(' ').first}.',
                            style: TextStyle(
                              fontSize: 28,
                              fontWeight: FontWeight.w800,
                              color: Theme.of(context).colorScheme.primary,
                            ),
                          ),
                          SizedBox(height: 8),
                          Text(
                            'Sua participação transforma o bairro.',
                            style: TextStyle(
                                color: Theme.of(context)
                                    .colorScheme
                                    .onSurfaceVariant),
                          ),
                          SizedBox(height: 24),
                          Container(
                            padding: EdgeInsets.all(24),
                            decoration: BoxDecoration(
                              color: Theme.of(context).colorScheme.primary,
                              borderRadius: BorderRadius.circular(8),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Icon(
                                  Icons.add_location_alt_outlined,
                                  color:
                                      Theme.of(context).colorScheme.onPrimary,
                                  size: 30,
                                ),
                                SizedBox(height: 16),
                                Text(
                                  'Encontrou um problema?',
                                  style: TextStyle(
                                    color:
                                        Theme.of(context).colorScheme.surface,
                                    fontSize: 21,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                                SizedBox(height: 8),
                                Text(
                                  'Conte o que aconteceu e acompanhe o atendimento.',
                                  style: TextStyle(
                                    color:
                                        Theme.of(context).colorScheme.onPrimary,
                                    height: 1.6,
                                  ),
                                ),
                                SizedBox(height: 20),
                                FilledButton(
                                  style: FilledButton.styleFrom(
                                    backgroundColor:
                                        Theme.of(context).colorScheme.primary,
                                    foregroundColor:
                                        Theme.of(context).colorScheme.onPrimary,
                                  ),
                                  onPressed: newOccurrence,
                                  child: Text('Nova reclamação'),
                                ),
                              ],
                            ),
                          ),
                          SizedBox(height: 24),
                          Row(
                            children: [
                              stat(
                                'Abertas',
                                all
                                    .where(
                                      (o) => [
                                        'Aberta',
                                        'Em análise',
                                      ].contains(o['status']),
                                    )
                                    .length,
                              ),
                              stat(
                                'Em andamento',
                                all
                                    .where((o) => o['status'] == 'Em andamento')
                                    .length,
                              ),
                              stat(
                                'Resolvidas',
                                all
                                    .where((o) => o['status'] == 'Resolvida')
                                    .length,
                              ),
                            ],
                          ),
                          SizedBox(height: 28),
                          Text(
                            'Últimas atualizações',
                            style: TextStyle(
                              fontSize: 19,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                          SizedBox(height: 12),
                        ] else ...[
                          Row(
                            children: [
                              Expanded(
                                child: Text(
                                  'Minhas reclamações',
                                  style: TextStyle(
                                    fontSize: 23,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                              ),
                              IconButton(
                                tooltip: 'Nova reclamação',
                                onPressed: newOccurrence,
                                icon: Icon(Icons.add_circle_outline),
                              ),
                            ],
                          ),
                          SizedBox(height: 18),
                          DropdownButtonFormField<String>(
                            initialValue: filter,
                            decoration: InputDecoration(labelText: 'Status'),
                            items: [
                              '',
                              'Aberta',
                              'Em análise',
                              'Em andamento',
                              'Resolvida',
                              'Cancelada',
                            ]
                                .map(
                                  (s) => DropdownMenuItem(
                                    value: s,
                                    child: Text(
                                      s.isEmpty ? 'Todos os status' : s,
                                    ),
                                  ),
                                )
                                .toList(),
                            onChanged: (v) => setState(() => filter = v ?? ''),
                          ),
                          SizedBox(height: 20),
                        ],
                        if ((tab == 0 ? all : items).isEmpty)
                          Padding(
                            padding: EdgeInsets.symmetric(vertical: 40),
                            child: Text(
                              'Nenhuma reclamação encontrada. Registre um problema para começar.',
                            ),
                          ),
                        ...(tab == 0
                                ? (List<Map<String, dynamic>>.from(all)
                                      ..sort(
                                        (a, b) => lastUpdate(b)
                                            .compareTo(lastUpdate(a)),
                                      ))
                                    .take(5)
                                : items)
                            .map(
                          (o) => Card(
                            elevation: 0,
                            color: Theme.of(context).colorScheme.surface,
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(8),
                              side: BorderSide(
                                  color: Theme.of(context)
                                      .colorScheme
                                      .outlineVariant),
                            ),
                            child: ListTile(
                              contentPadding: EdgeInsets.all(16),
                              onTap: () => detail(o),
                              title: Text(
                                o['categoria'].toString(),
                                style: TextStyle(
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              subtitle: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  SizedBox(height: 8),
                                  Text(
                                    '${o['bairro']} · ${dateLabel(o['dataAbertura'])}',
                                  ),
                                  SizedBox(height: 4),
                                  Text(
                                    'Protocolo ${o['protocolo']}',
                                    style: TextStyle(fontSize: 10),
                                  ),
                                  StatusChip(o['status'].toString()),
                                ],
                              ),
                              trailing: Icon(Icons.chevron_right),
                            ),
                          ),
                        ),
                        SizedBox(height: 24),
                        Text(
                          'Plataforma acadêmica fictícia · Santos/SP',
                          style: TextStyle(
                              fontSize: 11,
                              color: Theme.of(context)
                                  .colorScheme
                                  .onSurfaceVariant),
                        ),
                      ],
                    ),
                  );
                },
              ),
      );
  String lastUpdate(Map<String, dynamic> o) {
    final h = (o['historico'] as List?) ?? [];
    return h.isEmpty
        ? o['dataAbertura'].toString()
        : h.last['dataAlteracao'].toString();
  }

  Widget stat(String label, int value) => Expanded(
        child: Padding(
          padding: EdgeInsets.only(right: 8),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                '$value',
                style: TextStyle(
                  fontWeight: FontWeight.w800,
                  fontSize: 29,
                  color: Theme.of(context).colorScheme.primary,
                ),
              ),
              Text(
                label,
                style: TextStyle(
                    fontSize: 10,
                    color: Theme.of(context).colorScheme.onSurfaceVariant),
              ),
            ],
          ),
        ),
      );
}

class ProfileScreen extends StatefulWidget {
  final ApiClient api;
  final Map<String, dynamic> profile;
  final ValueChanged<Map<String, dynamic>> onProfile;
  final VoidCallback onLogout;
  const ProfileScreen({
    super.key,
    required this.api,
    required this.profile,
    required this.onProfile,
    required this.onLogout,
  });
  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  final key = GlobalKey<FormState>();
  late final TextEditingController name;
  late final TextEditingController phone;
  bool busy = false;
  String? message;
  @override
  void initState() {
    super.initState();
    name = TextEditingController(text: widget.profile['nome'].toString());
    phone = TextEditingController(text: widget.profile['telefone'].toString());
  }

  @override
  void dispose() {
    name.dispose();
    phone.dispose();
    super.dispose();
  }

  Future<void> save() async {
    if (!key.currentState!.validate()) return;
    setState(() => busy = true);
    try {
      final p = await PerfilService(widget.api)
          .editar(name.text.trim(), phone.text.trim());
      if (mounted) {
        widget.onProfile(p);
        setState(() => message = 'Perfil atualizado.');
      }
    } catch (e) {
      if (mounted) setState(() => message = e.toString());
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => ListView(
        padding: EdgeInsets.all(24),
        children: [
          Text(
            'Seu perfil',
            style: TextStyle(fontSize: 27, fontWeight: FontWeight.w800),
          ),
          SizedBox(height: 24),
          Text(widget.profile['email'].toString()),
          const SizedBox(height: 20),
          const AppearancePicker(),
          SizedBox(height: 24),
          Form(
            key: key,
            child: Column(
              children: [
                TextFormField(
                  controller: name,
                  decoration: InputDecoration(labelText: 'Nome completo'),
                  maxLength: 120,
                  validator: (v) =>
                      (v?.trim().length ?? 0) < 2 ? 'Informe o nome' : null,
                ),
                SizedBox(height: 18),
                TextFormField(
                  controller: phone,
                  decoration: InputDecoration(labelText: 'Telefone'),
                  keyboardType: TextInputType.phone,
                  validator: (v) =>
                      RegExp(r'^\+?[0-9 ()-]{10,20}$').hasMatch(v ?? '')
                          ? null
                          : 'Informe um telefone válido',
                ),
                SizedBox(height: 24),
                FilledButton(
                  onPressed: busy ? null : save,
                  child: Text(busy ? 'Salvando...' : 'Salvar alterações'),
                ),
              ],
            ),
          ),
          if (message != null)
            Padding(
              padding: EdgeInsets.symmetric(vertical: 18),
              child: Text(message!),
            ),
          SizedBox(height: 24),
          OutlinedButton(
            onPressed: widget.onLogout,
            child: Text('Sair da conta'),
          ),
          SizedBox(height: 24),
          Text(
            'Projeto acadêmico fictício. Sem vínculo com a Prefeitura de Santos.',
            style: TextStyle(fontSize: 12),
          ),
        ],
      );
}
