import 'api_client.dart';

class AuthService {
  final ApiClient api;
  AuthService(this.api);
  Future<Map<String, dynamic>> login(String email, String senha) async =>
      Map<String, dynamic>.from(
        await api.send(
          '/api/auth/login?publico=cidadao',
          method: 'POST',
          body: {'email': email, 'senha': senha},
        ),
      );
  Future<void> cadastro(
    String nome,
    String email,
    String telefone,
    String senha,
    String confirmarSenha,
  ) async {
    await api.send(
      '/api/auth/cadastro',
      method: 'POST',
      body: {
        'nome': nome,
        'email': email,
        'telefone': telefone,
        'senha': senha,
        'confirmarSenha': confirmarSenha,
      },
    );
  }

  Future<void> logout() async {
    try {
      await api.send('/api/auth/logout', method: 'POST', body: {});
    } finally {
      await api.clear();
    }
  }
}
