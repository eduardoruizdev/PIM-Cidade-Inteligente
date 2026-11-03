import 'package:flutter/material.dart';
import '../services/api_client.dart';
import 'weather_alerts_screen.dart';

String weatherLabel(dynamic code) {
  final c = (code as num?)?.toInt();
  if (c == null) return 'Condição indisponível';
  if (c == 0) return 'Céu limpo';
  if ([1, 2, 3].contains(c)) return 'Parcialmente nublado / nublado';
  if ([45, 48].contains(c)) return 'Nevoeiro';
  if ([51, 53, 55, 56, 57].contains(c)) return 'Garoa';
  if ([61, 63, 65, 66, 67].contains(c)) return 'Chuva';
  if ([71, 73, 75, 77].contains(c)) return 'Neve';
  if ([80, 81, 82].contains(c)) return 'Pancadas de chuva';
  if ([85, 86].contains(c)) return 'Pancadas de neve';
  if ([95, 96, 99].contains(c)) return 'Trovoadas';
  return 'Condição indisponível';
}

IconData weatherIcon(dynamic code) {
  final c = (code as num?)?.toInt() ?? -1;
  return c == 0
      ? Icons.wb_sunny_rounded
      : c >= 95
          ? Icons.thunderstorm
          : c >= 51
              ? Icons.water_drop
              : Icons.cloud;
}

String weatherTime(dynamic value) => value is num
    ? DateTime.fromMillisecondsSinceEpoch(value.toInt() * 1000, isUtc: true)
        .subtract(Duration(hours: 3))
        .toString()
        .substring(0, 16)
    : 'Indisponível';
String valueUnit(dynamic value, String unit) =>
    value is num ? '${value.toStringAsFixed(0)}$unit' : 'Indisponível';

class WeatherScreen extends StatefulWidget {
  final ApiClient api;
  const WeatherScreen({super.key, required this.api});
  @override
  State<WeatherScreen> createState() => _WeatherScreenState();
}

class _WeatherScreenState extends State<WeatherScreen> {
  late Future<dynamic> data;
  @override
  void initState() {
    super.initState();
    data = widget.api.send('/api/clima');
  }

  Future<void> reload() async {
    final next = widget.api.send('/api/clima');
    setState(() => data = next);
    await next;
  }

  @override
  Widget build(BuildContext context) => Scaffold(
      appBar: AppBar(title: Text('Clima em Santos'), actions: [
        IconButton(
            tooltip: 'Central de Alertas',
            icon: Icon(Icons.warning_amber),
            onPressed: () => Navigator.push(
                context,
                MaterialPageRoute(
                    builder: (_) => WeatherAlertsScreen(api: widget.api))))
      ]),
      body: FutureBuilder<dynamic>(
          future: data,
          builder: (context, s) {
            if (s.connectionState == ConnectionState.waiting) {
              return Center(child: CircularProgressIndicator());
            }
            if (s.hasError) {
              return Center(
                  child: Column(mainAxisSize: MainAxisSize.min, children: [
                Text('Clima indisponível. Verifique sua conexão.'),
                TextButton(
                    onPressed: () => reload(), child: Text('Tentar novamente'))
              ]));
            }
            final d = s.data['dados'];
            final c = d['current'];
            final h = d['hourly'];
            final days = d['daily'];
            final times = h['time'] as List;
            final now = DateTime.now().millisecondsSinceEpoch ~/ 1000;
            final upcoming = List.generate(times.length, (i) => i)
                .where((i) => (times[i] as num) >= now)
                .take(24)
                .toList();
            final currentIndex =
                times.indexWhere((t) => (t as num) >= (c['time'] as num));
            final stale = DateTime.now()
                .toUtc()
                .isAfter(DateTime.parse(s.data['validoAte']));
            return RefreshIndicator(
                onRefresh: reload,
                child: ListView(padding: EdgeInsets.all(20), children: [
                  if (stale)
                    Text('Dados desatualizados. Deslize para atualizar.'),
                  Card(
                      color: Theme.of(context).colorScheme.primaryContainer,
                      child: Padding(
                          padding: EdgeInsets.all(24),
                          child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('SANTOS / SP',
                                    style:
                                        TextStyle(fontWeight: FontWeight.bold)),
                                Icon(weatherIcon(c['weather_code']), size: 56),
                                Text(valueUnit(c['temperature_2m'], ' °C'),
                                    style: TextStyle(
                                        fontSize: 44,
                                        fontWeight: FontWeight.w800)),
                                Text(weatherLabel(c['weather_code'])),
                                Text(
                                    'Sensação térmica: ${valueUnit(c['apparent_temperature'], ' °C')}'),
                                SizedBox(height: 16),
                                Wrap(spacing: 20, runSpacing: 12, children: [
                                  Text(
                                      'Umidade: ${valueUnit(c['relative_humidity_2m'], '%')}'),
                                  Text(
                                      'Vento: ${valueUnit(c['wind_speed_10m'], ' km/h')}'),
                                  Text(
                                      'Chuva (hora): ${valueUnit(currentIndex >= 0 ? h['precipitation_probability'][currentIndex] : null, '%')}')
                                ]),
                              ]))),
                  SizedBox(height: 16),
                  OutlinedButton.icon(
                      onPressed: () => Navigator.push(
                          context,
                          MaterialPageRoute(
                              builder: (_) =>
                                  WeatherAlertsScreen(api: widget.api))),
                      icon: Icon(Icons.warning_amber),
                      label: Text('Central de Alertas')),
                  Text('Próximas horas',
                      style:
                          TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                  SizedBox(
                      height: 164,
                      child: ListView(
                          scrollDirection: Axis.horizontal,
                          children: upcoming
                              .map((i) => SizedBox(
                                  width: 116,
                                  child: Card(
                                      child: Padding(
                                          padding: EdgeInsets.all(12),
                                          child: Column(children: [
                                            Text(weatherTime(times[i])
                                                .substring(11)),
                                            Icon(weatherIcon(
                                                h['weather_code'][i])),
                                            Text(valueUnit(
                                                h['temperature_2m'][i], ' °C')),
                                            Text(
                                                'Chuva ${valueUnit(h['precipitation_probability'][i], '%')}')
                                          ])))))
                              .toList())),
                  SizedBox(height: 16),
                  Text('Próximos dias',
                      style:
                          TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                  ...List.generate(
                      (days['time'] as List).length,
                      (i) => Card(
                          child: ListTile(
                              leading:
                                  Icon(weatherIcon(days['weather_code'][i])),
                              title: Text(weatherTime(days['time'][i])
                                  .substring(0, 10)),
                              subtitle: Text(
                                  '${weatherLabel(days['weather_code'][i])}\nChuva: ${valueUnit(days['precipitation_probability_max'][i], '%')}'),
                              trailing: Text(
                                  '${valueUnit(days['temperature_2m_min'][i], '°')} / ${valueUnit(days['temperature_2m_max'][i], '°')}')))),
                  Text(
                      'Condição do modelo: ${weatherTime(c['time'])} (Santos, UTC−3)\nÚltima consulta: ${DateTime.parse(s.data['ultimaAtualizacao']).toLocal()}\n${s.data['fonte']}\nPrevisão pontual; pode variar entre bairros. Não substitui orientações oficiais.'),
                ]));
          }));
}
