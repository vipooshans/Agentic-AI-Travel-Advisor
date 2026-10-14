import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../models/user.dart';
import 'api_service.dart';

class AuthService {
  static const _tokenKey = 'access_token';
  static const _userKey = 'user_data';

  /// The mobile app only serves travelers; staff accounts use the web app.
  static const travelerRole = 'USER';
  static const travelersOnlyMessage =
      'This app is for travelers. Hotel owners, travel agents and administrators should sign in to the web app.';

  final ApiService _api;
  final FlutterSecureStorage _storage;

  AuthService({ApiService? api, FlutterSecureStorage? storage})
      : _api = api ?? ApiService(),
        _storage = storage ?? const FlutterSecureStorage();

  ApiService get api => _api;

  Future<AuthResponse> login(String email, String password) async {
    final response = await _api.post('/api/auth/login', {
      'email': email.trim(),
      'password': password,
    });
    _api.ensureSuccess(response);

    final auth = AuthResponse.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
    if (auth.user.role != travelerRole) {
      throw const ApiException(travelersOnlyMessage, statusCode: 403);
    }
    await _persistSession(auth);
    return auth;
  }

  Future<AuthResponse> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
  }) async {
    final response = await _api.post('/api/auth/register', {
      'email': email.trim(),
      'password': password,
      'firstName': firstName.trim(),
      'lastName': lastName.trim(),
    });
    _api.ensureSuccess(response);

    final auth = AuthResponse.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
    await _persistSession(auth);
    return auth;
  }

  Future<User> updateProfile({required String firstName, required String lastName}) async {
    final response = await _api.put('/api/auth/me', {
      'firstName': firstName.trim(),
      'lastName': lastName.trim(),
    });
    _api.ensureSuccess(response);
    final user = User.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    await _writeUser(user);
    return user;
  }

  Future<User?> getMe() async {
    final response = await _api.get('/api/auth/me');
    if (response.statusCode != 200) return null;

    return User.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<User?> restoreSession() async {
    final token = await _storage.read(key: _tokenKey);
    final userJson = await _storage.read(key: _userKey);

    if (token == null || userJson == null) return null;

    _api.setToken(token);
    final me = await getMe();
    if (me == null || me.role != travelerRole) {
      await logout();
      return null;
    }

    return me;
  }

  Future<void> logout() async {
    _api.setToken(null);
    await _storage.delete(key: _tokenKey);
    await _storage.delete(key: _userKey);
  }

  Future<void> _persistSession(AuthResponse auth) async {
    _api.setToken(auth.token);
    await _storage.write(key: _tokenKey, value: auth.token);
    await _writeUser(auth.user);
  }

  Future<void> _writeUser(User user) {
    return _storage.write(
      key: _userKey,
      value: jsonEncode({
        'id': user.id,
        'email': user.email,
        'firstName': user.firstName,
        'lastName': user.lastName,
        'role': user.role,
      }),
    );
  }
}
