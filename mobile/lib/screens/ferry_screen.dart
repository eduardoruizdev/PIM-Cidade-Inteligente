import 'dart:async';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../models/ferry_status.dart';
import '../services/api_client.dart';

class FerryScreen extends StatefulWidget {
  final ApiClient api;
  final Future<bool> Function(Uri)? openExternal;
  const FerryScreen({super.key, required this.api, this.openExternal});
  @override
  State<FerryScreen> createState() => _FerryScreenState();
}

class _FerryScreenState extends State<FerryScreen> with WidgetsBindingObserver {
  Timer? timer;
  FerryStatus? data;
  String? error;
  bool loading = false;
  DateTime now = DateTime.now().toUtc();
  ColorScheme get colors => Theme.of(context).colorScheme;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    reload();
    timer = Timer.periodic(const Duration(minutes: 1), (_) {
      if (ModalRoute.of(context)?.isCurrent == true &&
          WidgetsBinding.instance.lifecycleState == AppLifecycleState.resumed) {
        setState(() => now = DateTime.now().toUtc());
        if (++minutesSinceReload >= 5) reload();
      }
    });
  }

  int minutesSinceReload = 0;
  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed &&
        ModalRoute.of(context)?.isCurrent == true) {
      reload();
    }
  }

  @override
  void dispose() {
    timer?.cancel();
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  Future<void> reload() async {
    if (loading) return;
    setState(() => loading = true);
    minutesSinceReload = 0;
    try {
      final result = await widget.api.send('/api/travessias/santos-guaruja');
      final next =
          FerryStatus.fromJson(Map<String, dynamic>.from(result as Map));
      if (mounted) {
        setState(() {
          data = next;
          error = null;
          now = DateTime.now().toUtc();
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          error = 'Não foi possível consultar o serviço. Tente novamente.';
          now = DateTime.now().toUtc();
        });
      }
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> openSource() async {
    if (data?.linkCurrent(DateTime.now().toUtc()) != true) return;
    final uri = data!.externalUrl!;
    try {
      final opened = await (widget.openExternal?.call(uri) ??
          launchUrl(uri,
              mode: LaunchMode.externalApplication,
              webOnlyWindowName: '_blank'));
      if (opened) return;
    } catch (_) {/* Falha visível, sem WebView ou redirecionamento oculto. */}
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
          content: Text(
              'Não foi possível abrir a página no navegador. Tente novamente mais tarde.')));
    }
  }

  Widget pill(String text, IconData icon,
          {bool warning = false, bool attention = false}) => Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
      decoration: BoxDecoration(
          color:
              warning
                  ? colors.errorContainer
                  : attention
                      ? colors.tertiaryContainer
                      : colors.surfaceContainerHighest,
          borderRadius: BorderRadius.circular(24)),
      child: Row(mainAxisSize: MainAxisSize.min, children: [
        Icon(icon,
            size: 15,
            color: warning
                ? colors.onErrorContainer
                : attention
                    ? colors.onTertiaryContainer
                    : colors.onSurfaceVariant),
        const SizedBox(width: 6),
        Flexible(
            child: Text(text,
                style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.w600,
                    color: warning
                        ? colors.onErrorContainer
                        : attention
                            ? colors.onTertiaryContainer
                            : colors.onSurfaceVariant))),
      ]));

  Widget panel({required Widget child, Color? color}) => Container(
      padding: const EdgeInsets.all(22),
      decoration: BoxDecoration(
          color: color ?? colors.surfaceContainerLow,
          borderRadius: BorderRadius.circular(22),
          border: Border.all(color: colors.outlineVariant)),
      child: child);

  Widget header() {
    final dates = data?.directions
            .map((d) => d.measuredAt)
            .whereType<DateTime>()
            .toList() ??
        [];
    dates.sort();
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Row(children: [
        Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
                color: colors.primaryContainer,
                borderRadius: BorderRadius.circular(16)),
            child: Icon(Icons.directions_boat_rounded,
                color: colors.onPrimaryContainer, size: 28)),
        const SizedBox(width: 12),
        Expanded(
            child: Text('MOBILIDADE • TRAVESSIAS',
                style: TextStyle(
                    color: colors.primary,
                    fontSize: 11,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 1.2))),
      ]),
      const SizedBox(height: 20),
      Text('Travessia de Balsas',
          style: Theme.of(context).textTheme.headlineMedium?.copyWith(
              fontWeight: FontWeight.w800, height: 1.15, letterSpacing: -0.8)),
      const SizedBox(height: 10),
      Text('Acompanhe as condições da travessia Santos–Guarujá.',
          style: TextStyle(color: colors.onSurfaceVariant, height: 1.5)),
      const SizedBox(height: 16),
      Wrap(spacing: 8, runSpacing: 8, children: [
        pill(
            dates.isEmpty
                ? 'Sem atualização confirmada'
                : 'Medição mais recente: ${ferryTime(dates.last)}',
            Icons.schedule),
        pill('Veículos e passageiros', Icons.directions_car_outlined),
      ]),
    ]);
  }

  Widget crossing() => panel(
      color: colors.primaryContainer.withValues(alpha: 0.35),
      child: Column(children: [
        Row(children: [
          Icon(Icons.route_outlined, size: 18, color: colors.primary),
          const SizedBox(width: 8),
          Expanded(
              child: Text('Dois lados, uma conexão',
                  style: TextStyle(
                      fontWeight: FontWeight.w700, color: colors.primary))),
        ]),
        const SizedBox(height: 24),
        Row(children: [
          Icon(Icons.location_on, size: 22, color: colors.primary),
          Expanded(
              child: Divider(
                  color: colors.primary.withValues(alpha: 0.4),
                  indent: 8,
                  endIndent: 8)),
          Icon(Icons.directions_boat_outlined, size: 34, color: colors.primary),
          Expanded(
              child: Divider(
                  color: colors.primary.withValues(alpha: 0.4),
                  indent: 8,
                  endIndent: 8)),
          Icon(Icons.location_on_outlined, size: 22, color: colors.primary),
        ]),
        const SizedBox(height: 10),
        const Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
          Expanded(
              child: Text('Santos',
                  style: TextStyle(fontWeight: FontWeight.w700))),
          SizedBox(width: 12),
          Expanded(
              child: Text('Guarujá',
                  textAlign: TextAlign.end,
                  style: TextStyle(fontWeight: FontWeight.w700))),
        ]),
        const SizedBox(height: 10),
        Text('Esquema da travessia • sem rastreamento de embarcações',
            textAlign: TextAlign.center,
            style: TextStyle(
                color: colors.onSurfaceVariant, fontSize: 10, height: 1.5)),
      ]));

  Widget direction(FerryDirection d, int index) {
    final noticeText = data?.notices.join(' ').toLowerCase() ?? '';
    final interrupted = noticeText.contains('paralis') || noticeText.contains('interromp');
    final attention = noticeText.contains('lentidão') || noticeText.contains('maré forte');
    final fresh = d.current(now) && error == null && !interrupted && !attention;
    final historical = d.minutes != null &&
        (!d.current(now) || error != null) &&
        !attention &&
        !interrupted;
    return panel(
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
                color: colors.primaryContainer,
                borderRadius: BorderRadius.circular(12)),
            child: Icon(index == 0 ? Icons.north_east : Icons.south_west,
                size: 20, color: colors.onPrimaryContainer)),
        const SizedBox(width: 12),
        Expanded(
            child:
                Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text('SENTIDO ${index + 1}',
              style: TextStyle(
                  fontSize: 10,
                  color: colors.onSurfaceVariant,
                  letterSpacing: 1)),
          const SizedBox(height: 4),
          Text(d.name,
              style:
                  const TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
        ])),
      ]),
      const SizedBox(height: 24),
      Text(historical ? 'ÚLTIMA ESPERA INFORMADA' : 'TEMPO ESTIMADO DE ESPERA',
          style: TextStyle(
              fontSize: 10,
              fontWeight: FontWeight.w600,
              letterSpacing: 0.8,
              color: colors.onSurfaceVariant)),
      const SizedBox(height: 6),
      if (d.minutes != null)
        Text.rich(TextSpan(children: [
          TextSpan(
              text: '${d.minutes}',
              style: TextStyle(
                  fontSize: 46,
                  fontWeight: FontWeight.w800,
                  color: colors.onSurface)),
          TextSpan(
              text: ' min',
              style: TextStyle(fontSize: 18, color: colors.onSurfaceVariant)),
        ]))
      else ...[
        Text('Sem informação',
            style: TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.w800,
                color: colors.onSurface)),
        const SizedBox(height: 4),
        Text('Tempo de espera indisponível',
            style: TextStyle(color: colors.onSurfaceVariant, fontSize: 12)),
      ],
      const SizedBox(height: 14),
      pill(
          interrupted
              ? 'Interrupção informada pela fonte'
              : attention
                  ? 'Atenção operacional'
                  : fresh
              ? 'Atualização confirmada'
              : historical
                  ? 'Histórico · não usar como tempo atual'
                  : 'Aguardando dados verificados',
          interrupted
              ? Icons.block
              : attention
                  ? Icons.warning_amber_rounded
                  : fresh
              ? Icons.verified_outlined
              : historical
                  ? Icons.history
                  : Icons.info_outline,
          warning: historical || interrupted,
          attention: attention && !interrupted),
      const Padding(
          padding: EdgeInsets.symmetric(vertical: 16),
          child: Divider(height: 1)),
      Text('Operação${fresh ? '' : ' informada pela fonte'}',
          style: TextStyle(fontSize: 11, color: colors.onSurfaceVariant)),
      const SizedBox(height: 5),
      Text(d.operation ?? 'Situação não confirmada',
          style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600)),
      const SizedBox(height: 12),
      Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Icon(Icons.access_time, size: 15, color: colors.onSurfaceVariant),
        const SizedBox(width: 6),
        Expanded(
          child: Text('Página oficial consultada: ${ferryTime(d.measuredAt)}',
                style:
                    TextStyle(fontSize: 11, color: colors.onSurfaceVariant))),
      ]),
    ]));
  }

  Widget notice() {
    final unverified =
        data == null || data!.directions.any((d) => !d.current(now));
    final hasOperationalNotice = data?.notices.isNotEmpty == true;
    if (!unverified && error == null && !hasOperationalNotice) {
      return const SizedBox.shrink();
    }
    return panel(
        child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Icon(error == null ? Icons.info_outline : Icons.wifi_off_rounded,
          color: colors.primary),
      const SizedBox(width: 12),
      Expanded(
          child:
              Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(
            error == null
                ? hasOperationalNotice
                    ? 'Aviso operacional da AcquaVias'
                    : 'Planeje com informação confirmada'
                : 'Não foi possível atualizar',
            style: const TextStyle(fontWeight: FontWeight.w700)),
        const SizedBox(height: 8),
        Text(
            error ??
                (hasOperationalNotice
                    ? data!.notices.join('\n')
                    : data?.notice) ??
                'Buscando a disponibilidade das informações da travessia.',
            style: TextStyle(
                fontSize: 12, height: 1.6, color: colors.onSurfaceVariant)),
        if (error != null)
          TextButton(
              onPressed: loading ? null : reload,
              child: const Text('Tentar novamente')),
      ])),
    ]));
  }

  Widget details() => panel(
          child:
              Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text('Informações da consulta',
            style: Theme.of(context)
                .textTheme
                .titleMedium
                ?.copyWith(fontWeight: FontWeight.w700)),
        const SizedBox(height: 18),
        detail(Icons.source_outlined, 'Referência',
            data?.source ?? 'Aguardando consulta'),
        const SizedBox(height: 16),
        detail(
            Icons.directions_boat_outlined,
            'Embarcações em operação',
            data?.vessels == null
                ? 'Quantidade não informada'
                : '${data!.vessels} · informado pela fonte'),
        if (data?.notices.isNotEmpty == true) ...[
          const SizedBox(height: 16),
          detail(Icons.campaign_outlined, 'Avisos da fonte',
              data!.notices.join('\n')),
        ],
        const SizedBox(height: 20),
        if (data?.linkCurrent(now) == true) ...[
          OutlinedButton.icon(
              onPressed: openSource,
              icon: const Icon(Icons.open_in_new, size: 18),
              label: const Text('Consultar página externa')),
          const SizedBox(height: 8),
          Text(
              '${data!.externalName}\nAcesso verificado: ${ferryTime(data!.linkCheckedAt)}',
              style: TextStyle(
                  fontSize: 10, height: 1.6, color: colors.onSurfaceVariant)),
        ] else
          detail(Icons.link_off, 'Consulta externa indisponível',
              'Nenhum endereço pôde ser validado agora. Tente atualizar mais tarde.'),
      ]));

  Widget detail(IconData icon, String title, String value) =>
      Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Icon(icon, size: 20, color: colors.onSurfaceVariant),
        const SizedBox(width: 12),
        Expanded(
            child:
                Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(title,
              style: TextStyle(fontSize: 11, color: colors.onSurfaceVariant)),
          const SizedBox(height: 4),
          Text(value,
              style: const TextStyle(
                  fontSize: 12, height: 1.5, fontWeight: FontWeight.w500)),
        ])),
      ]);

  @override
  Widget build(BuildContext context) {
    final directions = data?.directions ??
        const [
          FerryDirection('Santos → Guarujá', null, null, null, false, false),
          FerryDirection('Guarujá → Santos', null, null, null, false, false),
        ];
    return Scaffold(
      appBar: AppBar(
          title: const Text('Travessia Santos–Guarujá',
              style: TextStyle(fontSize: 14))),
      body: RefreshIndicator(
          onRefresh: reload,
          child: SingleChildScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            child: Center(
                child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 840),
              child: Padding(
                  padding: const EdgeInsets.fromLTRB(20, 12, 20, 32),
                  child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        header(),
                        const SizedBox(height: 24),
                        crossing(),
                        const SizedBox(height: 20),
                        if (loading) ...[
                          const LinearProgressIndicator(
                              semanticsLabel: 'Consultando travessia'),
                          const SizedBox(height: 16),
                        ],
                        notice(),
                        const SizedBox(height: 20),
                        LayoutBuilder(
                            builder: (context, constraints) => constraints
                                        .maxWidth >=
                                    650
                                ? Row(
                                    crossAxisAlignment:
                                        CrossAxisAlignment.start,
                                    children: [
                                        Expanded(
                                            child: direction(directions[0], 0)),
                                        const SizedBox(width: 16),
                                        Expanded(
                                            child: direction(directions[1], 1)),
                                      ])
                                : Column(children: [
                                    direction(directions[0], 0),
                                    const SizedBox(height: 16),
                                    direction(directions[1], 1)
                                  ])),
                        const SizedBox(height: 20),
                        FilledButton.icon(
                            onPressed: loading ? null : reload,
                            icon: const Icon(Icons.refresh),
                            label: Text(loading
                                ? 'Atualizando consulta…'
                                : 'Atualizar consulta')),
                        const SizedBox(height: 10),
                        Text(
                            'Consulta automática a cada 5 minutos com esta tela ativa.',
                            textAlign: TextAlign.center,
                            style: TextStyle(
                                fontSize: 10, color: colors.onSurfaceVariant)),
                        const SizedBox(height: 24),
                        details(),
                        const SizedBox(height: 20),
                        panel(
                            child: detail(
                                Icons.timeline_rounded,
                                'Histórico de espera',
                                'Ainda não há medições verificadas suficientes para apresentar a evolução da espera.')),
                        const SizedBox(height: 20),
                        Text(
                            'Os tempos podem variar com a demanda e as condições de operação. Horários apresentados no fuso de São Paulo.',
                            textAlign: TextAlign.center,
                            style: TextStyle(
                                fontSize: 10,
                                height: 1.6,
                                color: colors.onSurfaceVariant)),
                      ])),
            )),
          )),
    );
  }
}
