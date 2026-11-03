import 'package:flutter/material.dart';

class Identity extends StatelessWidget {
  final bool light;
  const Identity({super.key, this.light = false});
  @override
  Widget build(BuildContext context) => Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            Icons.waves_rounded,
            size: 42,
            color: light
                ? Theme.of(context).colorScheme.surface
                : Theme.of(context).colorScheme.primary,
          ),
          SizedBox(width: 12),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Santos',
                style: TextStyle(
                  fontSize: 19,
                  color: light
                      ? Theme.of(context).colorScheme.surface
                      : Theme.of(context).colorScheme.primary,
                ),
              ),
              Text(
                'Conecta',
                style: TextStyle(
                  fontWeight: FontWeight.w800,
                  fontSize: 24,
                  color: light
                      ? Theme.of(context).colorScheme.surface
                      : Theme.of(context).colorScheme.primary,
                ),
              ),
            ],
          ),
        ],
      );
}

class StatusChip extends StatelessWidget {
  final String status;
  const StatusChip(this.status, {super.key});
  @override
  Widget build(BuildContext context) => Semantics(
      label: 'Status: $status',
      child: ExcludeSemantics(
          child: Chip(
        label: Text(status, style: TextStyle(fontSize: 11)),
        backgroundColor: status == 'Resolvida'
            ? Theme.of(context).colorScheme.primaryContainer
            : Theme.of(context).colorScheme.surfaceContainerHighest,
        side: BorderSide.none,
      )));
}

String dateLabel(dynamic value) {
  final d = DateTime.tryParse(value?.toString() ?? '')?.toLocal();
  return d == null
      ? 'Data indisponível'
      : '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';
}

String dateTimeLabel(dynamic value) {
  final d = DateTime.tryParse(value?.toString() ?? '')?.toLocal();
  return d == null
      ? 'Data indisponível'
      : '${dateLabel(value)} às ${d.hour.toString().padLeft(2, '0')}:${d.minute.toString().padLeft(2, '0')}';
}
