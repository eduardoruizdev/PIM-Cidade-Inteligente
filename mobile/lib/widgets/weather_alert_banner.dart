import 'dart:async';
import 'package:flutter/material.dart';
import '../services/api_client.dart';
import '../screens/weather_alerts_screen.dart';

class WeatherAlertBanner extends StatefulWidget {
  final ApiClient api;
  const WeatherAlertBanner({super.key, required this.api});
  @override
  State<WeatherAlertBanner> createState() => _WeatherAlertBannerState();
}

class _WeatherAlertBannerState extends State<WeatherAlertBanner> {
  List<dynamic> alerts = [];
  bool failed = false;
  Timer? timer;
  @override
  void initState() {
    super.initState();
    refresh();
    timer = Timer.periodic(Duration(minutes: 5), (_) => refresh());
  }

  Future<void> refresh() async {
    try {
      final result = await widget.api.send('/api/clima/alertas');
      if (mounted) {
        setState(() {
          alerts = result as List;
          failed = false;
        });
      }
    } catch (_) {
      if (mounted) setState(() => failed = true);
    }
  }

  @override
  void dispose() {
    timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (failed) {
      return Card(
          child: ListTile(
              leading: Icon(Icons.cloud_off),
              title: Text('Consulta de avisos indisponível')));
    }
    final valid = alerts
        .where((a) => DateTime.parse('${a['fim']}Z'.replaceAll('ZZ', 'Z'))
            .isAfter(DateTime.now().toUtc()))
        .toList();
    if (valid.isEmpty) return SizedBox.shrink();
    return Card(
        color: Theme.of(context).colorScheme.tertiaryContainer,
        child: ListTile(
            leading: Icon(Icons.warning_amber),
            title: Text('${valid.length} aviso(s) para Santos'),
            subtitle: Text(valid.first['titulo']),
            onTap: () => Navigator.push(
                context,
                MaterialPageRoute(
                    builder: (_) => WeatherAlertsScreen(api: widget.api)))));
  }
}
