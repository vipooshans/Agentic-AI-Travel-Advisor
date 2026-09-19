import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';

class ApiService {
  String? _token;

  void setToken(String? token) {
    _token = token;
  }

  Map<String, String> get _headers {
    final headers = {'Content-Type': 'application/json'};
    if (_token != null) {
      headers['Authorization'] = 'Bearer $_token';
    }
    return headers;
  }

  Future<http.Response> get(String path) {
    return http.get(
      Uri.parse('${ApiConfig.baseUrl}$path'),
      headers: _headers,
    );
  }

  Future<http.Response> post(String path, Map<String, dynamic> body, {Duration? timeout}) {
    final future = http.post(
      Uri.parse('${ApiConfig.baseUrl}$path'),
      headers: _headers,
      body: jsonEncode(body),
    );
    return timeout == null ? future : future.timeout(timeout);
  }

  String? parseErrorMessage(http.Response response) {
    try {
      final data = jsonDecode(response.body) as Map<String, dynamic>;
      return data['message'] as String?;
    } catch (_) {
      return 'Request failed (${response.statusCode})';
    }
  }
}
