import 'package:http/browser_client.dart';
import 'package:http/http.dart' as http;

// O navegador gerencia o cookie HttpOnly; o Dart não lê o ticket.
http.Client createPlatformClient() => BrowserClient()..withCredentials = true;
