import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mocktail/mocktail.dart';
import 'package:provider/provider.dart';
import 'package:travel_advisor/models/user.dart';
import 'package:travel_advisor/providers/auth_provider.dart';
import 'package:travel_advisor/providers/chat_provider.dart';
import 'package:travel_advisor/providers/session_binding.dart';
import 'package:travel_advisor/routes/app_router.dart';
import 'package:travel_advisor/services/api_service.dart';
import 'package:travel_advisor/services/auth_service.dart';

import 'helpers.dart';

/// HTTP verbs are mocked; error parsing stays real so screens show the same
/// messages they would for a real API response.
class MockApiService extends Mock implements ApiService {
  static final _real = ApiService(client: MockClient((_) async => http.Response('', 500)), baseUrl: testBaseUrl);

  @override
  void ensureSuccess(http.Response response, {Set<int> ok = const {200}}) => _real.ensureSuccess(response, ok: ok);

  @override
  String parseErrorMessage(http.Response response) => _real.parseErrorMessage(response);
}

class MockAuthService extends Mock implements AuthService {}

typedef GetHandler = http.Response Function(Map<String, Object?>? query);
typedef BodyHandler = http.Response Function(Map<String, dynamic> body);

const traveler = User(id: 'user-1', email: 'nimal@example.com', firstName: 'Nimal', lastName: 'Perera', role: 'USER');

void registerMockFallbacks() {
  registerFallbackValue(<String, dynamic>{});
}

/// The real app (providers, router, screens) on top of mocked services.
/// Routes not listed in [gets] / [posts] / [patches] answer 404 so a missing
/// stub shows up as a visible error instead of a hang.
class AppHarness {
  final api = MockApiService();
  final auth = MockAuthService();
  final gets = <String, GetHandler>{};
  final posts = <String, BodyHandler>{};
  final patches = <String, BodyHandler>{};
  late final AuthProvider authProvider;
  late final ChatProvider chat;
  late final GoRouter router;

  AppHarness({User? restoredUser = traveler}) {
    when(() => api.get(any(), query: any(named: 'query'), timeout: any(named: 'timeout'))).thenAnswer((i) async {
      final path = i.positionalArguments[0] as String;
      final handler = gets[path];
      return handler == null ? _missing('GET', path) : handler(i.namedArguments[#query] as Map<String, Object?>?);
    });
    when(() => api.post(any(), any(), timeout: any(named: 'timeout'))).thenAnswer((i) async {
      final path = i.positionalArguments[0] as String;
      final handler = posts[path];
      return handler == null ? _missing('POST', path) : handler(i.positionalArguments[1] as Map<String, dynamic>);
    });
    when(() => api.patch(any(), any(), timeout: any(named: 'timeout'))).thenAnswer((i) async {
      final path = i.positionalArguments[0] as String;
      final handler = patches[path];
      return handler == null ? _missing('PATCH', path) : handler(i.positionalArguments[1] as Map<String, dynamic>);
    });

    when(() => auth.api).thenReturn(api);
    when(() => auth.restoreSession()).thenAnswer((_) async => restoredUser);
    when(() => auth.logout()).thenAnswer((_) async {});

    authProvider = AuthProvider(auth);
    chat = ChatProvider();
    clearChatOnSignOut(authProvider, chat);
    router = createAppRouter(authProvider);
  }

  void onGet(String path, GetHandler handler) => gets[path] = handler;

  void onPost(String path, BodyHandler handler) => posts[path] = handler;

  static http.Response _missing(String verb, String path) => jsonResponse({'detail': 'No stub for $verb $path'}, 404);

  /// The location on screen, including pages opened with `context.push`.
  String get location {
    final matches = router.routerDelegate.currentConfiguration;
    final top = matches.matches.isEmpty ? null : matches.last;
    return (top is ImperativeRouteMatch ? top.matches.uri : matches.uri).toString();
  }

  /// Query maps sent with GETs to [path] since the last check, oldest first.
  /// Mocktail's `verify` only matches calls that have not been verified yet.
  List<Map<String, Object?>?> queriesFor(String path) => verify(
        () => api.get(path, query: captureAny(named: 'query'), timeout: any(named: 'timeout')),
      ).captured.cast<Map<String, Object?>?>();

  /// Bodies sent with POSTs to [path] since the last check, oldest first.
  List<Map<String, dynamic>> bodiesFor(String path) =>
      verify(() => api.post(path, captureAny(), timeout: any(named: 'timeout'))).captured.cast<Map<String, dynamic>>();

  Future<void> pump(WidgetTester tester, {String? goTo}) async {
    tester.view.physicalSize = const Size(1200, 2700);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(MultiProvider(
      providers: [
        ChangeNotifierProvider.value(value: authProvider),
        ChangeNotifierProvider.value(value: chat),
      ],
      child: TravelAdvisorApp(router: router),
    ));
    await tester.pumpAndSettle();
    if (goTo != null) await go(tester, goTo);
  }

  Future<void> go(WidgetTester tester, String location) async {
    router.go(location);
    await tester.pumpAndSettle();
  }

  Future<void> push(WidgetTester tester, String location) async {
    router.push(location);
    await tester.pumpAndSettle();
  }
}

Map<String, dynamic> destinationJson({int id = 6, String name = 'Ella', int? packageCount = 2}) => {
      'id': id,
      'name': name,
      'country': 'Sri Lanka',
      'description': '$name in the hill country.',
      'imageUrl': null,
      'packageCount': packageCount,
    };

Map<String, dynamic> roomJson({int id = 5, String name = 'Garden Double', double price = 8000, int capacity = 2, bool available = true}) => {
      'id': id,
      'hotelId': 3,
      'name': name,
      'roomType': 'Double',
      'pricePerNight': price,
      'capacity': capacity,
      'isAvailable': available,
    };

Map<String, dynamic> hotelJson({bool withRooms = false}) => {
      'id': 3,
      'name': 'Ella Gap View Inn',
      'address': '12 Passara Road',
      'city': 'Ella',
      'country': 'Sri Lanka',
      'description': 'Rooms facing the Ella Gap.',
      'imageUrl': null,
      'roomCount': 2,
      'minPricePerNight': 8000,
      'averageRating': 4.2,
      'reviewCount': 5,
      if (withRooms) 'rooms': [roomJson(), roomJson(id: 9, name: 'Closed Suite', price: 20000, available: false)],
    };

Map<String, dynamic> packageJson({bool withActivities = false}) => {
      'id': 7,
      'title': 'Ella Hill Country Escape',
      'description': 'Three days of tea country and waterfalls.',
      'price': 28000,
      'durationDays': 3,
      'destinationId': 6,
      'destinationName': 'Ella',
      'destinationCountry': 'Sri Lanka',
      'imageUrl': null,
      'activityCount': 2,
      'totalPrice': 36000,
      'maxTravelers': 10,
      'averageRating': null,
      'reviewCount': 0,
      if (withActivities)
        'activities': [
          {'id': 1, 'travelPackageId': 7, 'title': 'Nine Arches Bridge walk', 'description': 'Morning walk', 'dayNumber': 1, 'price': 0, 'sortOrder': 1},
          {'id': 2, 'travelPackageId': 7, 'title': 'Ravana Falls tour', 'description': null, 'dayNumber': 2, 'price': 8000, 'sortOrder': 2},
        ],
    };

Map<String, dynamic> transportJson() => {
      'id': 5,
      'mode': 1,
      'fromLocation': 'Kandy',
      'toLocation': 'Ella',
      'departureTime': '08:47:00',
      'durationMinutes': 400,
      'pricePerPerson': 2000,
      'capacity': 100,
    };

Map<String, dynamic> reviewJson() => {
      'id': 1,
      'bookingId': 11,
      'hotelId': 3,
      'rating': 5,
      'comment': 'Wonderful view from the balcony.',
      'authorName': 'Kamal S.',
      'createdAt': '2026-09-01T10:00:00Z',
    };

Map<String, dynamic> itineraryJson({int id = 12}) => {
      'id': id,
      'title': 'Ella trip',
      'startDate': '2026-10-10',
      'endDate': '2026-10-12',
      'status': 0,
      'estimatedCost': 76000,
      'destinationId': 6,
      'destinationName': 'Ella',
      'summary': '3 days in Ella for 2 travelers.',
      'itemCount': 3,
      'items': [
        {'id': 1, 'dayNumber': 2, 'title': 'Little Adam’s Peak hike', 'description': null, 'startTime': '06:30:00', 'sortOrder': 1},
        {'id': 2, 'dayNumber': 1, 'title': 'Train Kandy to Ella', 'description': 'Scenic route', 'startTime': '08:47:00', 'sortOrder': 1},
        {'id': 3, 'dayNumber': 1, 'title': 'Explore Ella town', 'description': null, 'startTime': null, 'sortOrder': 2},
      ],
    };
