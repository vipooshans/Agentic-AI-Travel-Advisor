// The real app against the real API and PostgreSQL database.
//
//   chromedriver --port=4444
//   flutter drive --driver=test_driver/integration_test.dart \
//     --target=integration_test/traveler_journey_test.dart \
//     -d web-server --browser-name=chrome --profile --web-port 4173 \
//     --dart-define=API_BASE_URL=http://localhost:5080
//
// Profile and release builds strip failure details from the driver output;
// run chromedriver with --verbose to see them in the browser console log.
// Port 4173 is one of the API's allowed CORS origins. Needs the Development
// demo accounts (override with IT_OWNER_* / IT_ADMIN_* dart-defines) and an
// auth rate limit above the default 10 sign-ins per minute.
//
// The tests run in order and share one traveler: each test restarts the app,
// which restores the session from secure storage.
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:travel_advisor/main.dart' as app;
import 'package:travel_advisor/widgets/booking_card.dart';
import 'package:travel_advisor/widgets/hotel_card.dart';

const baseUrl = String.fromEnvironment('API_BASE_URL', defaultValue: 'http://localhost:5080');
const ownerEmail = String.fromEnvironment('IT_OWNER_EMAIL', defaultValue: 'owner@traveladvisor.com');
const ownerPassword = String.fromEnvironment('IT_OWNER_PASSWORD', defaultValue: 'Owner@123');
const adminEmail = String.fromEnvironment('IT_ADMIN_EMAIL', defaultValue: 'admin@traveladvisor.com');
const adminPassword = String.fromEnvironment('IT_ADMIN_PASSWORD', defaultValue: 'Admin@123');
const travelerPassword = 'E2e#Traveler1';
const pricePerNight = 9500;
const planRequest = 'Plan a 3-day trip to Ella for 2 people with a budget of LKR 50000. We like hiking.';

final runId = DateTime.now().millisecondsSinceEpoch.toRadixString(36);
final hotelName = 'IT Mobile Hotel $runId';
final travelerEmail = 'it.mobile.$runId@example.test';

/// Direct API access, used only for setup and to check what the app wrote.
class Api {
  static Future<String> token(String email, String password) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email, 'password': password}),
    );
    expect(response.statusCode, 200, reason: 'login $email: ${response.body}');
    return (jsonDecode(response.body) as Map<String, dynamic>)['token'] as String;
  }

  static Future<(int, dynamic)> call(String token, String method, String path, [Object? body]) async {
    final request = http.Request(method, Uri.parse('$baseUrl$path'))
      ..headers['Authorization'] = 'Bearer $token'
      ..headers['Content-Type'] = 'application/json';
    if (body != null) request.body = jsonEncode(body);
    final response = await http.Response.fromStream(await request.send());
    return (response.statusCode, response.body.isEmpty ? null : jsonDecode(response.body));
  }
}

String isoDate(int daysFromToday) {
  final now = DateTime.now();
  final d = DateTime(now.year, now.month, now.day).add(Duration(days: daysFromToday));
  return '${d.year}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
}

/// Pumps until [finder] matches, for screens waiting on the network.
Future<void> pumpUntil(WidgetTester tester, Finder finder, {Duration timeout = const Duration(seconds: 30)}) async {
  final end = DateTime.now().add(timeout);
  while (DateTime.now().isBefore(end)) {
    await tester.pump(const Duration(milliseconds: 200));
    if (finder.evaluate().isNotEmpty) return;
  }
  final onScreen = find.byType(Text).evaluate().map((e) => (e.widget as Text).data ?? (e.widget as Text).textSpan?.toPlainText()).whereType<String>();
  throw TestFailure('Timed out after $timeout waiting for $finder. On screen: ${onScreen.join(' | ')}');
}

Future<void> pumpUntilGone(WidgetTester tester, Finder finder, {Duration timeout = const Duration(seconds: 15)}) async {
  final end = DateTime.now().add(timeout);
  while (DateTime.now().isBefore(end)) {
    await tester.pump(const Duration(milliseconds: 200));
    if (finder.evaluate().isEmpty) return;
  }
  throw TestFailure('Timed out after $timeout waiting for $finder to disappear');
}

Future<void> tap(WidgetTester tester, Finder finder) async {
  await tester.ensureVisible(finder.first);
  await tester.pump(const Duration(milliseconds: 300));
  await tester.tap(finder.first);
  await tester.pump(const Duration(milliseconds: 300));
}

Future<void> startApp(WidgetTester tester) async {
  // The integration binding leaves the simulated keyboard off, and without it
  // enterText silently types nothing in profile and release builds.
  tester.testTextInput.register();
  addTearDown(tester.testTextInput.unregister);
  app.main();
  await tester.pump();
}

Finder bookingCardFor(String title) =>
    find.ancestor(of: find.text(title), matching: find.byType(BookingCard));

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  late String ownerToken;
  late int hotelId;
  late int roomId;
  late int directBookingId;

  setUpAll(() async {
    ownerToken = await Api.token(ownerEmail, ownerPassword);
    final adminToken = await Api.token(adminEmail, adminPassword);
    final (hotelStatus, hotel) = await Api.call(ownerToken, 'POST', '/api/hotels', {
      'name': hotelName,
      'address': '8 Lighthouse Street',
      'city': 'Galle',
      'country': 'Sri Lanka',
    });
    expect(hotelStatus, 201, reason: '$hotel');
    hotelId = hotel['id'] as int;
    final (roomStatus, room) = await Api.call(ownerToken, 'POST', '/api/hotels/$hotelId/rooms', {
      'name': 'IT Sea View Double',
      'roomType': 'Double',
      'pricePerNight': pricePerNight,
      'capacity': 2,
    });
    expect(roomStatus, 201, reason: '$room');
    roomId = room['id'] as int;
    final (approval, _) = await Api.call(adminToken, 'PATCH', '/api/hotels/$hotelId/approval', {'status': 1});
    expect(approval, 200);
  });

  testWidgets('a new traveler registers, finds the hotel and books a room; the API stores it as Pending', (tester) async {
    await startApp(tester);
    await pumpUntil(tester, find.text('Welcome Back'));

    await tap(tester, find.text("Don't have an account? Register"));
    await pumpUntil(tester, find.text('Create Account'));
    await tester.enterText(find.widgetWithText(TextFormField, 'First Name'), 'Mobile');
    await tester.enterText(find.widgetWithText(TextFormField, 'Last Name'), 'Tester');
    await tester.enterText(find.widgetWithText(TextFormField, 'Email'), travelerEmail);
    await tester.enterText(find.widgetWithText(TextFormField, 'Password'), travelerPassword);
    await tester.enterText(find.widgetWithText(TextFormField, 'Confirm Password'), travelerPassword);
    await tap(tester, find.widgetWithText(FilledButton, 'Register'));
    await pumpUntil(tester, find.text('Hello, Mobile!'));

    await tap(tester, find.text('Hotels'));
    await pumpUntil(tester, find.widgetWithText(TextFormField, 'Hotel, city or country'));
    await tester.enterText(find.widgetWithText(TextFormField, 'Hotel, city or country'), hotelName);
    await tester.tap(find.byTooltip('Search'));
    // find.text also matches the search field, which holds the same name.
    final hotelResult = find.descendant(of: find.byType(HotelCard), matching: find.text(hotelName));
    await pumpUntil(tester, hotelResult);
    expect(find.text('from LKR 9,500'), findsOneWidget);

    await tap(tester, hotelResult);
    await pumpUntil(tester, find.text('IT Sea View Double'));
    expect(find.text('Double · 2 guests · LKR 9,500/night'), findsOneWidget);
    expect(find.text('No reviews yet.'), findsOneWidget);

    await tap(tester, find.widgetWithText(FilledButton, 'Book'));
    await pumpUntil(tester, find.text('Room Booking'));
    await tester.enterText(find.widgetWithText(TextFormField, 'Guests'), '2');
    await tap(tester, find.text('Check availability'));
    await pumpUntil(tester, find.text('Available'));
    expect(find.text('3 nights · 2 guests'), findsOneWidget);
    expect(find.text('Total LKR 28,500'), findsOneWidget);

    await tap(tester, find.text('Request booking for LKR 28,500'));
    final created = find.textContaining(RegExp(r'^Booking #\d+ created\. Status: Pending\.$'));
    await pumpUntil(tester, created);
    directBookingId = int.parse(RegExp(r'#(\d+)').firstMatch((tester.widget<Text>(created)).data!)!.group(1)!);
    await pumpUntil(tester, find.text('My Bookings'));

    final traveler = await Api.token(travelerEmail, travelerPassword);
    final (status, booking) = await Api.call(traveler, 'GET', '/api/bookings/$directBookingId');
    expect(status, 200);
    expect(booking['status'], 0, reason: 'Pending');
    expect(booking['roomId'], roomId);
    expect(booking['guests'], 2);
    expect((booking['totalPrice'] as num).toDouble(), 3 * pricePerNight);
    expect((booking['checkIn'] as String).substring(0, 10), isoDate(7));
    expect((booking['checkOut'] as String).substring(0, 10), isoDate(10));
    expect(booking['userEmail'], travelerEmail);
  });

  testWidgets('the assistant plans a trip, the plan is saved, and a booking exists only after Confirm', (tester) async {
    await startApp(tester);
    await pumpUntil(tester, find.text('Hello, Mobile!'));

    await tap(tester, find.byIcon(Icons.smart_toy_outlined));
    await pumpUntil(tester, find.text('Plan your next trip'));
    await tester.enterText(find.byType(TextField), planRequest);
    await tap(tester, find.byIcon(Icons.send));
    await pumpUntil(tester, find.text('Save itinerary'), timeout: const Duration(seconds: 90));
    expect(find.textContaining('Ella'), findsWidgets);

    await tap(tester, find.text('Save itinerary'));
    await pumpUntil(tester, find.text('Itinerary saved'));
    final traveler = await Api.token(travelerEmail, travelerPassword);
    final (_, itineraries) = await Api.call(traveler, 'GET', '/api/itineraries');
    expect(itineraries, hasLength(1), reason: 'the saved plan is stored for this traveler');
    // The snackbar's View action floats over the message box until it closes.
    await pumpUntilGone(tester, find.text('Itinerary saved'));

    await tester.enterText(find.byType(TextField), 'Book the hotel please');
    await tap(tester, find.byIcon(Icons.send));
    await pumpUntil(tester, find.text('Confirm booking'), timeout: const Duration(seconds: 90));
    expect(find.text('Nothing is booked until you confirm.'), findsOneWidget);
    final beforeConfirm = (await Api.call(traveler, 'GET', '/api/bookings')).$2 as List;
    expect(beforeConfirm, hasLength(1), reason: 'a proposal alone books nothing');

    await tap(tester, find.text('Confirm booking'));
    await pumpUntil(tester, find.text('The provider still has to confirm this booking.'), timeout: const Duration(seconds: 90));
    expect(find.text('Confirmed'), findsNothing);

    final afterConfirm = (await Api.call(traveler, 'GET', '/api/bookings')).$2 as List;
    expect(afterConfirm, hasLength(2));
    final aiBooking = afterConfirm.cast<Map<String, dynamic>>().firstWhere((b) => b['id'] != directBookingId);
    expect(aiBooking['status'], 0, reason: 'created as Pending');
    expect(find.text('Booking #${aiBooking['id']}'), findsOneWidget);
    final aiTitle = (aiBooking['packageTitle'] ?? aiBooking['hotelName']) as String;

    await tap(tester, find.text('View my bookings'));
    await pumpUntil(tester, bookingCardFor(aiTitle));
    await tap(tester, find.descendant(of: bookingCardFor(aiTitle), matching: find.text('Cancel booking')));
    await pumpUntil(tester, find.text('Cancel booking?'));
    await tap(tester, find.widgetWithText(FilledButton, 'Cancel booking'));
    await pumpUntil(tester, find.text('Booking cancelled'));

    final (_, cancelled) = await Api.call(traveler, 'GET', '/api/bookings/${aiBooking['id']}');
    expect(cancelled['status'], 2, reason: 'Cancelled in the database');
  });

  testWidgets('after the owner confirms, the app shows Confirmed; signing out ends the session and staff are refused', (tester) async {
    final (status, _) = await Api.call(ownerToken, 'PATCH', '/api/bookings/$directBookingId/status', {'status': 1});
    expect(status, 200);

    await startApp(tester);
    await pumpUntil(tester, find.text('Hello, Mobile!'));
    await tap(tester, find.byIcon(Icons.bookmark_outlined));
    await pumpUntil(tester, bookingCardFor(hotelName));
    expect(find.descendant(of: bookingCardFor(hotelName), matching: find.text('Confirmed')), findsOneWidget);
    expect(find.text('Cancelled'), findsOneWidget);

    await tap(tester, find.byIcon(Icons.person_outlined));
    await pumpUntil(tester, find.text(travelerEmail));
    await tap(tester, find.text('Sign Out'));
    await pumpUntil(tester, find.text('Welcome Back'));

    await tester.enterText(find.widgetWithText(TextFormField, 'Email'), ownerEmail);
    await tester.enterText(find.widgetWithText(TextFormField, 'Password'), ownerPassword);
    await tap(tester, find.widgetWithText(FilledButton, 'Sign In'));
    await pumpUntil(tester, find.textContaining('This app is for travelers'));
    expect(find.text('Hello, Mobile!'), findsNothing);
  });
}
