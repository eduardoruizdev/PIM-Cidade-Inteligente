/// Contrato da API: horário da consulta nunca substitui o horário da medição.
class FerryDirection {
  final String name;
  final int? minutes;
  final String? operation;
  final DateTime? measuredAt;
  final bool available;
  final bool stale;
  const FerryDirection(this.name, this.minutes, this.operation, this.measuredAt,
      this.available, this.stale);

  factory FerryDirection.fromJson(Map<String, dynamic> json, String name) {
    final value = json['esperaMinutos'];
    final minutes = value is num &&
            value.isFinite &&
            value == value.round() &&
            value >= 0 &&
            value <= 1440
        ? value.toInt()
        : null;
    final measuredAt =
        DateTime.tryParse(json['informadoEm']?.toString() ?? '')?.toUtc();
    return FerryDirection(
        name,
        minutes,
        json['situacaoOperacional'] as String?,
        measuredAt,
        json['disponivel'] == true && minutes != null,
        json['desatualizado'] == true);
  }

  bool current(DateTime now) =>
      available &&
      !stale &&
      measuredAt != null &&
      !measuredAt!.isAfter(now.toUtc().add(const Duration(minutes: 2))) &&
      now.toUtc().difference(measuredAt!) <= const Duration(minutes: 15);
}

class FerryStatus {
  final String source;
  final String notice;
  final List<FerryDirection> directions;
  final int? vessels;
  final List<String> notices;
  final Uri? externalUrl;
  final String? externalName;
  final DateTime? linkCheckedAt;
  const FerryStatus(this.source, this.notice, this.directions, this.vessels,
      this.notices, this.externalUrl, this.externalName, this.linkCheckedAt);

  factory FerryStatus.fromJson(Map<String, dynamic> json) {
    final rows = (json['sentidos'] as List? ?? []).whereType<Map>();
    const names = ['Santos → Guarujá', 'Guarujá → Santos'];
    final directions = names.map((name) {
      final matches = rows.where((row) => row['sentido'] == name);
      return FerryDirection.fromJson(
          matches.isEmpty ? {} : Map<String, dynamic>.from(matches.first),
          name);
    }).toList();
    final link =
        json['consultaExterna'] is Map ? json['consultaExterna'] as Map : {};
    final uri = Uri.tryParse(link['url']?.toString() ?? '');
    // Somente destinos conhecidos e efetivamente validados pelo backend.
    final checked =
        DateTime.tryParse(link['verificadoEm']?.toString() ?? '')?.toUtc();
    final trusted = link['disponivel'] == true &&
        checked != null &&
        uri?.scheme == 'https' &&
        uri!.userInfo.isEmpty &&
        ['semil.sp.gov.br', 'metroa.com.br', 'acquavias.com.br'].contains(uri.host);
    final vessels = json['embarcacoes'];
    return FerryStatus(
        json['fonte'] as String? ?? 'SEMIL · Departamento Hidroviário',
        json['aviso'] as String? ??
            'Não foi possível confirmar os tempos de espera agora.',
        directions,
        vessels is int && vessels >= 0 ? vessels : null,
        (json['comunicados'] as List? ?? []).whereType<String>().toList(),
        trusted ? uri : null,
        link['nome'] as String?,
        checked);
  }

  bool linkCurrent(DateTime now) =>
      externalUrl != null &&
      linkCheckedAt != null &&
      !linkCheckedAt!.isAfter(now.toUtc().add(const Duration(minutes: 2))) &&
      now.toUtc().difference(linkCheckedAt!) < const Duration(minutes: 30);
}

String ferryTime(DateTime? value) {
  if (value == null) return 'Não informada';
  // São Paulo (UTC−3), independente do fuso configurado no aparelho.
  final local = value.toUtc().subtract(const Duration(hours: 3));
  String two(int n) => n.toString().padLeft(2, '0');
  return '${two(local.day)}/${two(local.month)} ${two(local.hour)}:${two(local.minute)} · SP';
}
