import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';

/// An error the UI can show to the user as-is.
class ApiException implements Exception {
  final String message;
  final int? statusCode;

  const ApiException(this.message, {this.statusCode});

  @override
  String toString() => message;
}

class ApiService {
  static const defaultTimeout = Duration(seconds: 20);
  static const offlineMessage = 'Unable to reach the server. Check your connection and try again.';
  static const timeoutMessage = 'The server took too long to respond. Please try again.';
  static const forbiddenMessage = 'You do not have permission to do that.';

  final http.Client _client;
  final String? _baseUrl;
  final Duration _timeout;
  String? _token;

  /// Called when an authenticated request is rejected with 401, i.e. the
  /// stored token has expired or been revoked.
  void Function()? onUnauthorized;

  ApiService({http.Client? client, String? baseUrl, Duration timeout = defaultTimeout})
      : _client = client ?? http.Client(),
        _baseUrl = baseUrl,
        _timeout = timeout;

  bool get hasToken => _token != null;

  void setToken(String? token) {
    _token = token;
  }

  Map<String, String> get _headers {
    final headers = {'Content-Type': 'application/json', 'Accept': 'application/json'};
    if (_token != null) {
      headers['Authorization'] = 'Bearer $_token';
    }
    return headers;
  }

  /// Builds a URL for [path], dropping null or blank query values.
  Uri uri(String path, [Map<String, Object?>? query]) {
    final base = Uri.parse('${_baseUrl ?? ApiConfig.baseUrl}$path');
    final params = <String, String>{
      ...base.queryParameters,
      if (query != null)
        for (final entry in query.entries)
          if (entry.value != null && entry.value.toString().trim().isNotEmpty)
            entry.key: entry.value.toString().trim(),
    };
    return params.isEmpty ? base : base.replace(queryParameters: params);
  }

  Future<http.Response> get(String path, {Map<String, Object?>? query, Duration? timeout}) {
    return _send(path, timeout, () => _client.get(uri(path, query), headers: _headers));
  }

  Future<http.Response> post(String path, Map<String, dynamic> body, {Duration? timeout}) {
    return _send(path, timeout, () => _client.post(uri(path), headers: _headers, body: jsonEncode(body)));
  }

  Future<http.Response> put(String path, Map<String, dynamic> body, {Duration? timeout}) {
    return _send(path, timeout, () => _client.put(uri(path), headers: _headers, body: jsonEncode(body)));
  }

  Future<http.Response> patch(String path, Map<String, dynamic> body, {Duration? timeout}) {
    return _send(path, timeout, () => _client.patch(uri(path), headers: _headers, body: jsonEncode(body)));
  }

  Future<http.Response> delete(String path, {Duration? timeout}) {
    return _send(path, timeout, () => _client.delete(uri(path), headers: _headers));
  }

  Future<http.Response> _send(String path, Duration? timeout, Future<http.Response> Function() request) async {
    final sentToken = _token != null;
    final http.Response response;
    try {
      response = await request().timeout(timeout ?? _timeout);
    } on TimeoutException {
      throw const ApiException(timeoutMessage);
    } on SocketException {
      throw const ApiException(offlineMessage);
    } on http.ClientException {
      throw const ApiException(offlineMessage);
    }

    if (response.statusCode == 401 && sentToken && !_isCredentialEndpoint(path)) {
      onUnauthorized?.call();
    }
    return response;
  }

  static bool _isCredentialEndpoint(String path) =>
      path.startsWith('/api/auth/login') || path.startsWith('/api/auth/register');

  /// Throws an [ApiException] with the server's message unless the response
  /// status is one of [ok].
  void ensureSuccess(http.Response response, {Set<int> ok = const {200}}) {
    if (!ok.contains(response.statusCode)) {
      throw ApiException(parseErrorMessage(response), statusCode: response.statusCode);
    }
  }

  /// Reads the ProblemDetails body returned by the API: field errors first,
  /// then `detail`, then the legacy `message` field.
  String parseErrorMessage(http.Response response) {
    final fallback = _statusMessage(response.statusCode);
    try {
      final data = jsonDecode(response.body);
      if (data is! Map<String, dynamic>) return fallback;

      final errors = data['errors'];
      final details = <String>{
        if (errors is List) ...errors.map((e) => e.toString()),
        if (errors is Map)
          for (final value in errors.values)
            if (value is List) ...value.map((e) => e.toString()) else value.toString(),
      };
      if (details.length > 1) return details.join('\n');

      final detail = data['detail'];
      if (detail is String && detail.trim().isNotEmpty) return detail;
      final message = data['message'];
      if (message is String && message.trim().isNotEmpty) return message;
      if (details.isNotEmpty) return details.first;
      return fallback;
    } catch (_) {
      return fallback;
    }
  }

  static String _statusMessage(int status) {
    if (status == 400) return 'Please check the details you entered.';
    if (status == 401) return 'Please sign in again.';
    if (status == 403) return forbiddenMessage;
    if (status == 404) return 'We could not find what you were looking for.';
    if (status == 409) return 'That change conflicts with existing data.';
    if (status == 429) return 'Too many requests. Please wait a moment and try again.';
    if (status >= 500) return 'The server had a problem. Please try again later.';
    return 'Request failed ($status).';
  }
}
