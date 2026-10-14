import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:travel_advisor/models/chat_message.dart';
import 'package:travel_advisor/providers/auth_provider.dart';
import 'package:travel_advisor/providers/chat_provider.dart';
import 'package:travel_advisor/providers/session_binding.dart';
import 'package:travel_advisor/services/api_service.dart';
import 'package:travel_advisor/services/auth_service.dart';

import 'helpers.dart';

const storage = FlutterSecureStorage();

void main() {
  setUp(() => FlutterSecureStorage.setMockInitialValues({}));

  group('AuthService role gating', () {
    test('a traveler signs in and the session is stored', () async {
      final api = fakeApi((_) async => jsonResponse(authJson()));
      final auth = await AuthService(api: api, storage: storage).login(' nimal@example.com ', 'Travel#2026');

      expect(auth.user.role, 'USER');
      expect(api.hasToken, isTrue);
      expect(await storage.read(key: 'access_token'), 'token-abc');
    });

    for (final role in ['HOTEL_OWNER', 'TRAVEL_AGENT', 'ADMIN']) {
      test('$role is refused and nothing is stored', () async {
        final api = fakeApi((_) async => jsonResponse(authJson(role: role)));
        final service = AuthService(api: api, storage: storage);

        await expectLater(
          service.login('staff@traveladvisor.com', 'Owner@123'),
          throwsA(isA<ApiException>().having((e) => e.message, 'message', AuthService.travelersOnlyMessage)),
        );
        expect(api.hasToken, isFalse);
        expect(await storage.read(key: 'access_token'), isNull);
        expect(await storage.read(key: 'user_data'), isNull);
      });
    }

    test('a stored staff session is discarded on restore', () async {
      FlutterSecureStorage.setMockInitialValues({
        'access_token': 'old-token',
        'user_data': jsonEncode(userJson(role: 'TRAVEL_AGENT')),
      });
      final api = fakeApi((_) async => jsonResponse(userJson(role: 'TRAVEL_AGENT')));

      final user = await AuthService(api: api, storage: storage).restoreSession();

      expect(user, isNull);
      expect(api.hasToken, isFalse);
      expect(await storage.read(key: 'access_token'), isNull);
    });

    test('a valid traveler session is restored', () async {
      FlutterSecureStorage.setMockInitialValues({
        'access_token': 'good-token',
        'user_data': jsonEncode(userJson()),
      });
      String? sentAuth;
      final api = fakeApi((request) async {
        sentAuth = request.headers['Authorization'];
        return jsonResponse(userJson());
      });

      final user = await AuthService(api: api, storage: storage).restoreSession();

      expect(user?.email, 'nimal@example.com');
      expect(sentAuth, 'Bearer good-token');
    });

    test('the registration error from the API is shown as-is', () async {
      final api = fakeApi((_) async => jsonResponse({
            'detail': "Passwords must have at least one non alphanumeric character.",
          }, 400));
      await expectLater(
        AuthService(api: api, storage: storage).register(
          email: 'new@example.com',
          password: 'Abcdef1',
          firstName: 'New',
          lastName: 'User',
        ),
        throwsA(isA<ApiException>().having((e) => e.message, 'message', contains('non alphanumeric'))),
      );
    });
  });

  group('AuthProvider session expiry', () {
    test('staff sign-in leaves the provider signed out with the travelers-only message', () async {
      final api = fakeApi((_) async => jsonResponse(authJson(role: 'ADMIN')));
      final provider = AuthProvider(AuthService(api: api, storage: storage));

      final ok = await provider.login('admin@traveladvisor.com', 'Admin@123');

      expect(ok, isFalse);
      expect(provider.isAuthenticated, isFalse);
      expect(provider.error, AuthService.travelersOnlyMessage);
    });

    test('a 401 after sign-in signs the user out, clears storage and explains why', () async {
      var expired = false;
      final api = fakeApi((request) async {
        if (request.url.path == '/api/auth/login') return jsonResponse(authJson());
        return expired ? http.Response('', 401) : jsonResponse([]);
      });
      final provider = AuthProvider(AuthService(api: api, storage: storage));
      await provider.login('nimal@example.com', 'Travel#2026');
      expect(provider.isAuthenticated, isTrue);

      expired = true;
      await api.get('/api/bookings');
      await Future<void>.delayed(Duration.zero);

      expect(provider.status, AuthStatus.unauthenticated);
      expect(provider.user, isNull);
      expect(provider.notice, AuthProvider.sessionExpiredMessage);
      expect(api.hasToken, isFalse);
      expect(await storage.read(key: 'access_token'), isNull);
    });

    test('an expired stored token on start-up explains why the user must sign in', () async {
      FlutterSecureStorage.setMockInitialValues({
        'access_token': 'expired',
        'user_data': jsonEncode(userJson()),
      });
      final api = fakeApi((_) async => http.Response('', 401));
      final provider = AuthProvider(AuthService(api: api, storage: storage));

      await provider.initialize();

      expect(provider.status, AuthStatus.unauthenticated);
      expect(provider.notice, AuthProvider.sessionExpiredMessage);
    });

    test('signing in again clears the notice', () async {
      var expired = false;
      final api = fakeApi((request) async {
        if (request.url.path == '/api/auth/login') return jsonResponse(authJson());
        return expired ? http.Response('', 401) : jsonResponse([]);
      });
      final provider = AuthProvider(AuthService(api: api, storage: storage));
      await provider.login('nimal@example.com', 'Travel#2026');
      expired = true;
      await api.get('/api/bookings');
      expect(provider.notice, isNotNull);

      await provider.login('nimal@example.com', 'Travel#2026');
      expect(provider.notice, isNull);
      expect(provider.isAuthenticated, isTrue);
    });
  });

  group('clearChatOnSignOut', () {
    test('the conversation is wiped when the user signs out', () async {
      final api = fakeApi((_) async => jsonResponse(authJson()));
      final auth = AuthProvider(AuthService(api: api, storage: storage));
      final chat = ChatProvider();
      clearChatOnSignOut(auth, chat);

      await auth.login('nimal@example.com', 'Travel#2026');
      chat.loadConversation(7, const [ChatMessage(role: 'user', content: 'My passport number is N1234567')]);
      expect(chat.hasState, isTrue);

      await auth.logout();

      expect(chat.messages, isEmpty);
      expect(chat.conversationId, isNull);
    });
  });
}
