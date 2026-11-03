import 'package:flutter/material.dart';

import '../services/api_client.dart';
import '../services/auth_service.dart';
import '../widgets/identity.dart';
import '../services/appearance.dart';

class WelcomeScreen extends StatelessWidget {
  final ApiClient api;
  final ValueChanged<Map<String, dynamic>> onLogin;
  final String? error;
  const WelcomeScreen({
    super.key,
    required this.api,
    required this.onLogin,
    this.error,
  });
  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(actions: const [AppearanceButton()]),
        body: SafeArea(
          child: Padding(
            padding: EdgeInsets.all(28),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                SizedBox(height: 30),
                Identity(),
                Spacer(),
                Icon(
                  Icons.location_city_outlined,
                  size: 90,
                  color: Theme.of(context).colorScheme.primary,
                ),
                SizedBox(height: 24),
                Text(
                  'Sua cidade,\nmais perto de você.',
                  style: TextStyle(
                    fontSize: 35,
                    fontWeight: FontWeight.w800,
                    height: 1.15,
                    color: Theme.of(context).colorScheme.primary,
                  ),
                ),
                SizedBox(height: 20),
                Text(
                  'Registre um problema no bairro e acompanhe cada atualização, do protocolo à solução.',
                  style: TextStyle(
                    fontSize: 16,
                    height: 1.7,
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
                ),
                Spacer(),
                if (error != null)
                  Padding(
                    padding: EdgeInsets.only(bottom: 12),
                    child: Text(error!, style: TextStyle(color: Colors.red)),
                  ),
                FilledButton(
                  onPressed: () => open(context, false),
                  child: Text('Entrar'),
                ),
                SizedBox(height: 12),
                OutlinedButton(
                  style: OutlinedButton.styleFrom(
                    minimumSize: Size(double.infinity, 50),
                  ),
                  onPressed: () => open(context, true),
                  child: Text('Criar conta'),
                ),
                SizedBox(height: 24),
                Text(
                  'Projeto acadêmico fictício. Sem vínculo com a Prefeitura de Santos.',
                  style: TextStyle(
                      fontSize: 11,
                      color: Theme.of(context).colorScheme.onSurfaceVariant),
                ),
                SizedBox(height: 10),
              ],
            ),
          ),
        ),
      );
  void open(BuildContext context, bool register) {
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => AuthScreen(
          api: api,
          register: register,
          onLogin: (p) {
            Navigator.of(context).pop();
            onLogin(p);
          },
        ),
      ),
    );
  }
}

class AuthScreen extends StatefulWidget {
  final ApiClient api;
  final bool register;
  final ValueChanged<Map<String, dynamic>> onLogin;
  const AuthScreen({
    super.key,
    required this.api,
    required this.register,
    required this.onLogin,
  });
  @override
  State<AuthScreen> createState() => _AuthScreenState();
}

class _AuthScreenState extends State<AuthScreen> {
  final key = GlobalKey<FormState>();
  final name = TextEditingController();
  final email = TextEditingController();
  final phone = TextEditingController();
  final password = TextEditingController();
  final confirmation = TextEditingController();
  bool busy = false;
  String? error;
  bool created = false;
  @override
  void dispose() {
    for (final c in [name, email, phone, password, confirmation]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> submit() async {
    if (!key.currentState!.validate()) return;
    setState(() => busy = true);
    try {
      final service = AuthService(widget.api);
      if (widget.register && !created) {
        await service.cadastro(
          name.text.trim(),
          email.text.trim(),
          phone.text.trim(),
          password.text,
          confirmation.text,
        );
        created = true;
      }
      final p = await service.login(email.text.trim(), password.text);
      password.clear();
      confirmation.clear();
      if (mounted) widget.onLogin(p);
    } catch (e) {
      if (mounted) setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(
            title: Text(widget.register ? 'Criar conta' : 'Entrar'),
            actions: const [AppearanceButton()]),
        body: SingleChildScrollView(
          padding: EdgeInsets.all(24),
          child: Form(
            key: key,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Identity(),
                SizedBox(height: 32),
                Text(
                  widget.register
                      ? 'Vamos cuidar da cidade?'
                      : 'Bom ter você por aqui.',
                  style: TextStyle(fontWeight: FontWeight.w700, fontSize: 25),
                ),
                SizedBox(height: 12),
                Text(
                  'Sua participação ajuda a organizar o atendimento urbano.',
                ),
                SizedBox(height: 24),
                if (widget.register) ...[
                  field(
                    name,
                    'Nome completo',
                    validator: (v) =>
                        (v?.trim().length ?? 0) < 2 ? 'Informe seu nome' : null,
                  ),
                  field(
                    phone,
                    'Telefone',
                    type: TextInputType.phone,
                    validator: (v) =>
                        RegExp(r'^\+?[0-9 ()-]{10,20}$').hasMatch(v ?? '')
                            ? null
                            : 'Informe um telefone válido',
                  ),
                ],
                field(
                  email,
                  'E-mail',
                  type: TextInputType.emailAddress,
                  validator: (v) =>
                      RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$').hasMatch(v ?? '')
                          ? null
                          : 'Informe um e-mail válido',
                ),
                field(
                  password,
                  'Senha',
                  secret: true,
                  validator: (v) => (v?.length ?? 0) <
                          (widget.register ? 12 : 8)
                      ? 'Use pelo menos ${widget.register ? 12 : 8} caracteres'
                      : null,
                ),
                if (widget.register)
                  field(
                    confirmation,
                    'Confirmar senha',
                    secret: true,
                    validator: (v) =>
                        v != password.text ? 'As senhas não coincidem' : null,
                  ),
                if (error != null)
                  Padding(
                    padding: EdgeInsets.only(bottom: 16),
                    child: Text(
                      error!,
                      style:
                          TextStyle(color: Theme.of(context).colorScheme.error),
                    ),
                  ),
                FilledButton(
                  onPressed: busy ? null : submit,
                  child: Text(
                    busy
                        ? 'Aguarde...'
                        : widget.register
                            ? 'Criar conta e entrar'
                            : 'Entrar',
                  ),
                ),
                SizedBox(height: 20),
                Text(
                  'Seus dados são usados apenas neste projeto acadêmico. Não informe dados sensíveis nas reclamações.',
                  style: TextStyle(fontSize: 12, height: 1.6),
                ),
              ],
            ),
          ),
        ),
      );
  Widget field(
    TextEditingController c,
    String label, {
    bool secret = false,
    TextInputType? type,
    String? Function(String?)? validator,
  }) =>
      Padding(
        padding: EdgeInsets.only(bottom: 18),
        child: TextFormField(
          controller: c,
          decoration: InputDecoration(labelText: label),
          obscureText: secret,
          keyboardType: type,
          autocorrect: !secret,
          enableSuggestions: !secret,
          maxLength: secret ? 128 : null,
          validator: validator,
        ),
      );
}
