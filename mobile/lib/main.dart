import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart';

import 'services/api_client.dart';
import 'services/appearance.dart';
import 'services/weather_push.dart';
import 'services/auth_service.dart';
import 'services/perfil_service.dart';
import 'screens/auth_screens.dart';
import 'screens/home_screen.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Appearance.instance.load();
  runApp(const SantosApp());
}

class SantosApp extends StatefulWidget {
  const SantosApp({super.key});
  @override
  State<SantosApp> createState() => _SantosAppState();
}

class _SantosAppState extends State<SantosApp> {
  final api = ApiClient();
  Map<String, dynamic>? profile;
  bool starting = true;
  String? error;
  @override
  void initState() {
    super.initState();
    restore();
  }

  Future<void> restore() async {
    try {
      await api.restore();
      profile = await PerfilService(api).me();
    } on ApiException catch (e) {
      if (e.status != 401) error = e.message;
    } catch (_) {
      error = 'Não foi possível recuperar a sessão.';
    }
    if (mounted) setState(() => starting = false);
  }

  Future<void> logout() async {
    try {
      await WeatherPush.configure(api, false);
    } catch (_) {/* Sem rede: revogar no próximo acesso. */}
    try {
      await AuthService(api).logout();
    } catch (_) {
      /* Sessão local apagada mesmo sem rede. */
    }
    if (mounted) setState(() => profile = null);
  }

  @override
  Widget build(BuildContext context) => ListenableBuilder(
      listenable: Appearance.instance,
      builder: (context, _) => MaterialApp(
            debugShowCheckedModeBanner: false,
            title: 'Santos Conecta',
            builder: (context, child) => kIsWeb
                ? ColoredBox(
                    color: Theme.of(context).colorScheme.surfaceContainer,
                    child: Center(
                      child: ConstrainedBox(
                        constraints: const BoxConstraints(maxWidth: 480),
                        child: child ?? const SizedBox.shrink(),
                      ),
                    ),
                  )
                : child ?? const SizedBox.shrink(),
            theme: santosTheme(Brightness.light),
            darkTheme: santosTheme(Brightness.dark),
            themeMode: Appearance.instance.mode,
            home: starting
                ? const Scaffold(
                    body: Center(child: CircularProgressIndicator()))
                : profile == null
                    ? WelcomeScreen(
                        api: api,
                        error: error,
                        onLogin: (p) => setState(() {
                          profile = p;
                          error = null;
                        }),
                      )
                    : HomeScreen(
                        api: api,
                        profile: profile!,
                        onLogout: logout,
                        onProfile: (p) => setState(() => profile = p),
                      ),
          ));
}
