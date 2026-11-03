import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class Appearance extends ChangeNotifier {
  static final instance = Appearance();
  final FlutterSecureStorage storage;
  ThemeMode mode = ThemeMode.system;
  Appearance({FlutterSecureStorage? storage})
      : storage = storage ?? const FlutterSecureStorage();
  Future<void> load() async {
    try {
      final value = await storage.read(key: 'appearance');
      mode = ThemeMode.values
          .firstWhere((m) => m.name == value, orElse: () => ThemeMode.system);
    } catch (_) {
      mode = ThemeMode.system;
    }
    notifyListeners();
  }

  Future<bool> select(ThemeMode value) async {
    try {
      await storage.write(key: 'appearance', value: value.name);
      mode = value;
      notifyListeners();
      return true;
    } catch (_) {
      return false;
    }
  }
}

ThemeData santosTheme(Brightness brightness) {
  final colors = ColorScheme.fromSeed(
      seedColor: const Color(0xff25431d), brightness: brightness);
  return ThemeData(
    useMaterial3: true,
    fontFamily: 'Montserrat',
    brightness: brightness,
    colorScheme: colors,
    scaffoldBackgroundColor: colors.surface,
    appBarTheme: AppBarTheme(
        backgroundColor: colors.surface,
        foregroundColor: colors.onSurface,
        surfaceTintColor: Colors.transparent),
    navigationBarTheme: NavigationBarThemeData(
        backgroundColor: colors.surfaceContainer,
        indicatorColor: colors.primaryContainer),
    inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: colors.surfaceContainerLow,
        border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(10),
            borderSide: BorderSide(color: colors.outline))),
    listTileTheme: ListTileThemeData(
        iconColor: colors.primary,
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 20, vertical: 8)),
    filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
            minimumSize: const Size(double.infinity, 50),
            shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8)))),
  );
}

class AppearancePicker extends StatelessWidget {
  const AppearancePicker({super.key});
  @override
  Widget build(BuildContext context) => ListenableBuilder(
      listenable: Appearance.instance,
      builder: (context, _) => DropdownButtonFormField<ThemeMode>(
            isExpanded: true,
            key: ValueKey(Appearance.instance.mode),
            initialValue: Appearance.instance.mode,
            decoration: const InputDecoration(
                labelText: 'Aparência', prefixIcon: Icon(Icons.contrast)),
            items: const [
              DropdownMenuItem(value: ThemeMode.light, child: Text('Claro')),
              DropdownMenuItem(value: ThemeMode.dark, child: Text('Escuro')),
              DropdownMenuItem(
                  value: ThemeMode.system, child: Text('Automático (sistema)'))
            ],
            onChanged: (value) async {
              if (value == null) return;
              final saved = await Appearance.instance.select(value);
              if (!saved && context.mounted) {
                ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
                    content: Text('Não foi possível salvar a aparência.')));
              }
            },
          ));
}

class AppearanceButton extends StatelessWidget {
  const AppearanceButton({super.key});
  @override
  Widget build(BuildContext context) =>
      IconButton(
          tooltip: 'Aparência',
          icon: const Icon(Icons.contrast),
          onPressed: () => showDialog<void>(
              context: context,
              builder: (context) => AlertDialog(
                      title: const Text('Aparência'),
                      content:
                          const SizedBox(width: 300, child: AppearancePicker()),
                      actions: [
                        TextButton(
                            onPressed: () => Navigator.pop(context),
                            child: const Text('Fechar'))
                      ])));
}
