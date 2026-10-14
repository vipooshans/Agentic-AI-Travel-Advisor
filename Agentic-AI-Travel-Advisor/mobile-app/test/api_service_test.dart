import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:travel_advisor/services/api_service.dart';

import 'helpers.dart';

void main() {
  group('ApiService.uri', () {
    test('adds non-empty query values and drops null or blank ones', () {
      final api = fakeApi((_) async => jsonResponse([]));
      final uri = api.uri('/api/hotels', {'q': ' Ella ', 'city': null, 'maxPrice': 20000.0, 'country': '  '});

      expect(uri.path, '/api/hotels');
      expect(uri.queryParameters, {'q': 'Ella', 'maxPrice': '20000.0'});
    });

    test('encodes special characters instead of concatenating raw strings', () {
      final api = fakeApi((_) async => jsonResponse([]));
      final uri = api.uri('/api/hotels', {'q': 'Nuwara Eliya & Co'});
      expect(uri.toString(), contains('q=Nuwara+Eliya+%26+Co'));
    });
  });

  group('ApiService.parseErrorMessage', () {
    final api = fakeApi((_) async => jsonResponse(null));

    test('prefers the ProblemDetails detail field', () {
      final message = api.parseErrorMessage(jsonResponse({
        'title': 'Conflict',
        'detail': 'Room is already booked for the selected dates.',
        'message': 'legacy message',
      }, 409));
      expect(message, 'Room is already booked for the selected dates.');
    });

    test('lists every field error when there is more than one', () {
      final message = api.parseErrorMessage(jsonResponse({
        'detail': 'Guests must be between 1 and 50.',
        'errors': {
          'guests': ['Guests must be between 1 and 50.'],
          'checkOut': ['CheckOut must be after CheckIn.'],
        },
      }, 400));
      expect(message, 'Guests must be between 1 and 50.\nCheckOut must be after CheckIn.');
    });

    test('falls back to the legacy message field', () {
      expect(api.parseErrorMessage(jsonResponse({'message': 'Invalid email or password.'}, 401)), 'Invalid email or password.');
    });

    test('uses a friendly message for an empty 403 body', () {
      expect(api.parseErrorMessage(http.Response('', 403)), ApiService.forbiddenMessage);
    });

    test('uses a friendly message for a non-JSON server error', () {
      expect(api.parseErrorMessage(http.Response('<html>oops</html>', 502)), 'The server had a problem. Please try again later.');
    });
  });

  group('ApiService requests', () {
    test('sends the bearer token once set', () async {
      String? auth;
      final api = fakeApi((request) async {
        auth = request.headers['Authorization'];
        return jsonResponse({});
      });
      await api.get('/api/bookings');
      expect(auth, isNull);

      api.setToken('abc');
      await api.get('/api/bookings');
      expect(auth, 'Bearer abc');
    });

    test('reports a 401 on an authenticated request as an expired session', () async {
      var calls = 0;
      final api = fakeApi((_) async => http.Response('', 401));
      api.onUnauthorized = () => calls++;
      api.setToken('expired');

      final response = await api.get('/api/bookings');
      expect(response.statusCode, 401);
      expect(calls, 1);
    });

    test('does not treat a failed sign-in or an anonymous 401 as an expired session', () async {
      var calls = 0;
      final api = fakeApi((_) async => jsonResponse({'detail': 'Invalid email or password.'}, 401));
      api.onUnauthorized = () => calls++;

      await api.get('/api/bookings');
      api.setToken('stale');
      await api.post('/api/auth/login', {'email': 'a@b.co', 'password': 'x'});
      await api.post('/api/auth/register', {'email': 'a@b.co', 'password': 'x'});
      expect(calls, 0);
    });

    test('times out slow requests with a readable message', () async {
      final api = fakeApi(
        (_) => Completer<http.Response>().future,
        timeout: const Duration(milliseconds: 20),
      );
      await expectLater(
        api.get('/api/hotels'),
        throwsA(isA<ApiException>().having((e) => e.message, 'message', ApiService.timeoutMessage)),
      );
    });

    test('allows a longer timeout per request', () async {
      final api = fakeApi(
        (_) => Future.delayed(const Duration(milliseconds: 60), () => jsonResponse({'ok': true})),
        timeout: const Duration(milliseconds: 20),
      );
      final response = await api.post('/api/ai/chat', {}, timeout: const Duration(seconds: 2));
      expect(response.statusCode, 200);
    });

    test('turns connection failures into an offline message', () async {
      final api = fakeApi((_) async => throw http.ClientException('Connection refused'));
      await expectLater(
        api.get('/api/hotels'),
        throwsA(isA<ApiException>().having((e) => e.message, 'message', ApiService.offlineMessage)),
      );
    });

    test('ensureSuccess throws the server message with the status code', () {
      final api = fakeApi((_) async => jsonResponse(null));
      expect(
        () => api.ensureSuccess(jsonResponse({'detail': 'You have already reviewed this booking.'}, 409)),
        throwsA(isA<ApiException>()
            .having((e) => e.message, 'message', 'You have already reviewed this booking.')
            .having((e) => e.statusCode, 'statusCode', 409)),
      );
      expect(() => api.ensureSuccess(jsonResponse({}, 201), ok: const {200, 201}), returnsNormally);
    });
  });
}
