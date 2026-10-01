import 'dart:async';

import 'package:flutter/foundation.dart';

import '../models/user.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';

enum AuthStatus { unknown, authenticated, unauthenticated }

class AuthProvider extends ChangeNotifier {
  static const sessionExpiredMessage = 'Your session has expired. Please sign in again.';

  final AuthService _authService;

  AuthProvider(this._authService) {
    _authService.api.onUnauthorized = _handleUnauthorized;
  }

  AuthStatus _status = AuthStatus.unknown;
  User? _user;
  bool _isLoading = false;
  String? _error;
  String? _notice;

  AuthStatus get status => _status;
  User? get user => _user;
  bool get isLoading => _isLoading;
  String? get error => _error;

  /// An informational message for the sign-in screen, e.g. why the user was
  /// signed out.
  String? get notice => _notice;
  bool get isAuthenticated => _status == AuthStatus.authenticated;
  ApiService get api => _authService.api;

  Future<void> initialize() async {
    _isLoading = true;
    notifyListeners();

    try {
      final user = await _authService.restoreSession();
      if (user != null) {
        _user = user;
        _status = AuthStatus.authenticated;
      } else {
        _status = AuthStatus.unauthenticated;
      }
    } catch (_) {
      _status = AuthStatus.unauthenticated;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> login(String email, String password) async {
    _isLoading = true;
    _error = null;
    _notice = null;
    notifyListeners();

    try {
      final auth = await _authService.login(email, password);
      _user = auth.user;
      _status = AuthStatus.authenticated;
      return true;
    } catch (e) {
      _error = _messageOf(e);
      return false;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
  }) async {
    _isLoading = true;
    _error = null;
    _notice = null;
    notifyListeners();

    try {
      final auth = await _authService.register(
        email: email,
        password: password,
        firstName: firstName,
        lastName: lastName,
      );
      _user = auth.user;
      _status = AuthStatus.authenticated;
      return true;
    } catch (e) {
      _error = _messageOf(e);
      return false;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> updateProfile({required String firstName, required String lastName}) async {
    _isLoading = true;
    _error = null;
    notifyListeners();
    try {
      _user = await _authService.updateProfile(firstName: firstName, lastName: lastName);
      return true;
    } catch (e) {
      _error = _messageOf(e);
      return false;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> logout() async {
    await _authService.logout();
    _user = null;
    _error = null;
    _notice = null;
    _status = AuthStatus.unauthenticated;
    notifyListeners();
  }

  void clearError() {
    _error = null;
    notifyListeners();
  }

  void _handleUnauthorized() {
    if (_status == AuthStatus.unauthenticated) return;
    unawaited(_authService.logout());
    _user = null;
    _notice = sessionExpiredMessage;
    _status = AuthStatus.unauthenticated;
    notifyListeners();
  }

  static String _messageOf(Object error) => error.toString().replaceFirst('Exception: ', '');
}
