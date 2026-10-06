// Contract checks of the app's services against a running API.
//
//   flutter test test_live --dart-define=API_BASE_URL=http://localhost:5080
//
// Needs the seeded demo data and a traveler account; credentials can be
// overridden with LIVE_USER_EMAIL / LIVE_USER_PASSWORD dart-defines.
//
// Kept outside test/ so the default `flutter test` run stays offline, which
// means the analyzer does not treat this file as test code.
// ignore_for_file: invalid_use_of_visible_for_testing_member
@Tags(['live'])
library;

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:travel_advisor/models/booking.dart';
import 'package:travel_advisor/providers/auth_provider.dart';
import 'package:travel_advisor/providers/chat_provider.dart';
import 'package:travel_advisor/services/api_service.dart';
import 'package:travel_advisor/services/auth_service.dart';
import 'package:travel_advisor/services/booking_service.dart';
import 'package:travel_advisor/services/hotel_service.dart';
import 'package:travel_advisor/services/package_service.dart';
import 'package:travel_advisor/services/review_service.dart';
import 'package:travel_advisor/services/transportation_service.dart';

const baseUrl = String.fromEnvironment('API_BASE_URL', defaultValue: 'http://localhost:5080');
const userEmail = String.fromEnvironment('LIVE_USER_EMAIL', defaultValue: 'nimal.p6.react@example.com');
const userPassword = String.fromEnvironment('LIVE_USER_PASSWORD', defaultValue: 'Travel#2026');
const ownerEmail = 'owner@traveladvisor.com';
const ownerPassword = 'Owner@123';
const storage = FlutterSecureStorage();

ApiService newApi() => ApiService(baseUrl: baseUrl);

void main() {
  // The API allows 10 sign-ins per minute per IP, so one traveler session is
  // shared by the tests that only need to be signed in.
  late ApiService traveler;
  Future<ApiService> signedInTraveler() async => traveler;

  setUpAll(() async {
    FlutterSecureStorage.setMockInitialValues({});
    traveler = newApi();
    await AuthService(api: traveler, storage: storage).login(userEmail, userPassword);
  });

  setUp(() => FlutterSecureStorage.setMockInitialValues({}));

  test('a hotel owner is refused by the mobile app and nothing is stored', () async {
    final api = newApi();
    await expectLater(
      AuthService(api: api, storage: storage).login(ownerEmail, ownerPassword),
      throwsA(isA<ApiException>().having((e) => e.message, 'message', AuthService.travelersOnlyMessage)),
    );
    expect(api.hasToken, isFalse);
    expect(await storage.read(key: 'access_token'), isNull);
    printOnFailure('owner refused');
  });

  test('wrong credentials show the API message', () async {
    await expectLater(
      AuthService(api: newApi(), storage: storage).login(userEmail, 'wrong-password'),
      throwsA(isA<ApiException>().having((e) => e.statusCode, 'statusCode', 401)),
    );
  });

  test('a traveler signs in and is told when the token stops working', () async {
    final api = newApi();
    final auth = AuthProvider(AuthService(api: api, storage: storage));
    expect(await auth.login(userEmail, userPassword), isTrue, reason: auth.error);
    expect(auth.user?.role, 'USER');

    api.setToken('not-a-valid-jwt');
    final response = await api.get('/api/bookings');

    expect(response.statusCode, 401);
    expect(auth.status, AuthStatus.unauthenticated);
    expect(auth.notice, AuthProvider.sessionExpiredMessage);
    expect(await storage.read(key: 'access_token'), isNull);
  });

  test('a traveler calling an admin API gets the permission message', () async {
    final api = await signedInTraveler();
    final response = await api.get('/api/users');
    expect(response.statusCode, 403);
    expect(() => api.ensureSuccess(response), throwsA(isA<ApiException>().having((e) => e.statusCode, 'statusCode', 403)));
  });

  test('hotel search filters by keyword and price', () async {
    final hotels = HotelService(newApi());
    final all = await hotels.getAll();
    expect(all, isNotEmpty);

    final city = all.first.city;
    final byKeyword = await hotels.getAll(q: city);
    expect(byKeyword, isNotEmpty);
    for (final h in byKeyword) {
      final haystack = '${h.name} ${h.city} ${h.country} ${h.address} ${h.description ?? ''}'.toLowerCase();
      expect(haystack, contains(city.toLowerCase()));
    }

    final prices = all.map((h) => h.minPricePerNight).whereType<double>().toList()..sort();
    final cap = prices[prices.length ~/ 2];
    final cheap = await hotels.getAll(maxPrice: cap);
    expect(cheap.length, lessThanOrEqualTo(all.length));
    for (final h in cheap) {
      expect(h.minPricePerNight, isNotNull);
      expect(h.minPricePerNight!, lessThanOrEqualTo(cap));
    }
    printOnFailure('hotels=${all.length} keyword[$city]=${byKeyword.length} maxPrice[$cap]=${cheap.length}');
  });

  test('package search filters by the per-person price shown in the app', () async {
    final packages = PackageService(newApi());
    final all = await packages.getAll();
    expect(all, isNotEmpty);
    final cap = all.map((p) => p.pricePerPerson).reduce((a, b) => a < b ? a : b);
    final cheapest = await packages.getAll(maxPrice: cap);
    expect(cheapest, isNotEmpty);
    for (final p in cheapest) {
      expect(p.pricePerPerson, lessThanOrEqualTo(cap));
    }
  });

  test('reviews, transport and the traveler\'s own data parse from the API', () async {
    final api = await signedInTraveler();
    final hotel = (await HotelService(api).getAll()).first;
    final package = (await PackageService(api).getAll()).first;

    final hotelReviews = await ReviewService(api).forHotel(hotel.id);
    final packageReviews = await ReviewService(api).forPackage(package.id);
    final mine = await ReviewService(api).mine();
    final transport = await TransportationService(api).search(destinationId: package.destinationId);
    final bookings = await BookingService(api).getAll();

    expect(hotelReviews.length, lessThanOrEqualTo(hotel.reviewCount == 0 ? hotelReviews.length : hotel.reviewCount));
    for (final r in [...hotelReviews, ...packageReviews, ...mine]) {
      expect(r.rating, inInclusiveRange(1, 5));
    }
    for (final t in transport) {
      expect(t.modeLabel, isNot('Transport'));
    }
    expect(bookings.every((b) => b.userId == bookings.first.userId), isTrue, reason: 'a traveler only sees their own bookings');
  });

  test('availability check, booking with guests, and cancellation', () async {
    final api = await signedInTraveler();
    final hotels = HotelService(api);
    final summary = (await hotels.getAll()).firstWhere((h) => h.roomCount > 0);
    final hotel = await hotels.getById(summary.id);
    final room = hotel.rooms!.firstWhere((r) => r.isAvailable && r.capacity >= 2);

    final offset = 200 + DateTime.now().millisecondsSinceEpoch % 100;
    final today = DateTime.now();
    final checkIn = DateTime(today.year, today.month, today.day).add(Duration(days: offset));
    final checkOut = checkIn.add(const Duration(days: 2));
    final bookings = BookingService(api);

    final tooMany = await bookings
        .checkAvailability(roomId: room.id, checkIn: checkIn, checkOut: checkOut, guests: room.capacity + 1)
        .then((q) => q.available ? 'available' : (q.reason ?? 'unavailable'), onError: (Object e) => e.toString());
    expect(tooMany, isNot('available'), reason: 'more guests than the room holds');

    final quote = await bookings.checkAvailability(roomId: room.id, checkIn: checkIn, checkOut: checkOut, guests: 2);
    expect(quote.available, isTrue, reason: quote.reason);
    expect(quote.nights, 2);

    final booking = await bookings.create(roomId: room.id, checkIn: checkIn, checkOut: checkOut, guests: 2, notes: 'Phase 7 live check');
    expect(booking.status, Booking.pending);
    expect(booking.guests, 2);
    expect(booking.totalPrice, quote.totalPrice);

    final cancelled = await bookings.updateStatus(booking.id, Booking.cancelled);
    expect(cancelled.status, Booking.cancelled);
    printOnFailure('booking #${booking.id} room ${room.id} ${quote.totalPrice}');
  });

  test('the assistant returns a structured plan that the app can read', () async {
    final api = await signedInTraveler();
    final chat = ChatProvider();
    await chat.send(api, 'Plan a 3-day trip to Ella for 2 people under LKR 150000');

    expect(chat.error, isNull);
    final reply = chat.messages.last;
    expect(reply.isUser, isFalse);
    expect(reply.status, isNotNull);
    expect(reply.status, isNot('booking_created'), reason: 'nothing is booked without confirmation');
    if (reply.plan != null) {
      expect(reply.plan!.destination.toLowerCase(), contains('ella'));
      expect(reply.plan!.travelers, 2);
      expect(reply.plan!.toItineraryRequest()['items'], isA<List>());
    }
    printOnFailure('status=${reply.status} plan=${reply.plan != null} proposal=${reply.pendingBooking != null}');
  }, timeout: const Timeout(Duration(minutes: 2)));
}
