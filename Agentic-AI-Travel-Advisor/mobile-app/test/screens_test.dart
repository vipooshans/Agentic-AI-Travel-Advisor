import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';
import 'package:travel_advisor/models/booking.dart';
import 'package:travel_advisor/models/chat_message.dart';
import 'package:travel_advisor/models/review.dart';
import 'package:travel_advisor/models/travel_plan.dart';
import 'package:travel_advisor/providers/auth_provider.dart';
import 'package:travel_advisor/screens/booking_list_screen.dart';
import 'package:travel_advisor/screens/create_booking_screen.dart';
import 'package:travel_advisor/screens/destination_list_screen.dart';
import 'package:travel_advisor/screens/login_screen.dart';
import 'package:travel_advisor/screens/register_screen.dart';
import 'package:travel_advisor/services/auth_service.dart';
import 'package:travel_advisor/services/booking_service.dart';
import 'package:travel_advisor/widgets/booking_proposal_card.dart';
import 'package:travel_advisor/widgets/catalog_search_bar.dart';
import 'package:travel_advisor/widgets/chat_bubble.dart';
import 'package:travel_advisor/widgets/reviews_section.dart';
import 'package:travel_advisor/widgets/travel_plan_card.dart';
import 'package:travel_advisor/widgets/write_review_sheet.dart';

import 'helpers.dart';

Widget _scrollable(Widget child) => MaterialApp(home: Scaffold(body: SingleChildScrollView(child: child)));

void main() {
  setUp(() => FlutterSecureStorage.setMockInitialValues({}));

  group('Assistant UI', () {
    testWidgets('the plan card shows costs against budget, stay, package, activities, transport and days', (tester) async {
      var saved = false;
      await tester.pumpWidget(_scrollable(TravelPlanCard(plan: TravelPlan.fromJson(planJson()), onSave: () => saved = true)));

      expect(find.text('Ella trip'), findsOneWidget);
      expect(find.text('Estimated LKR 76,000'), findsOneWidget);
      expect(find.text('Within budget'), findsOneWidget);
      expect(find.text('Budget LKR 80,000'), findsOneWidget);
      expect(find.text('Ella Gap View Inn'), findsOneWidget);
      expect(find.text('Ella Hill Country Escape'), findsOneWidget);
      expect(find.text('Day 2: Little Adam’s Peak hike (included)'), findsOneWidget);
      expect(find.textContaining('Train: Kandy → Ella at 08:47'), findsOneWidget);
      expect(find.text('Day 1 · 10 Oct 2026'), findsOneWidget);
      expect(find.text('08:47  Train Kandy to Ella'), findsOneWidget);
      expect(find.text('Trains fill up quickly on weekends.'), findsOneWidget);

      await tester.ensureVisible(find.text('Save itinerary'));
      await tester.tap(find.text('Save itinerary'));
      expect(saved, isTrue);
    });

    testWidgets('an over-budget plan is labelled as such', (tester) async {
      final json = planJson()
        ..['withinBudget'] = false
        ..['estimatedTotal'] = 95000;
      await tester.pumpWidget(_scrollable(TravelPlanCard(plan: TravelPlan.fromJson(json))));

      expect(find.text('Over budget'), findsOneWidget);
      expect(find.text('Save itinerary'), findsNothing);
    });

    testWidgets('a proposal can be confirmed only when it is the latest and unexpired', (tester) async {
      final proposal = BookingProposal.fromJson(proposalJson(expiresAt: DateTime.utc(2026, 10, 1, 12)));
      final beforeExpiry = DateTime.utc(2026, 10, 1, 11);
      var confirmed = false;

      await tester.pumpWidget(_scrollable(BookingProposalCard(
        proposal: proposal,
        canConfirm: true,
        onConfirm: () => confirmed = true,
        now: beforeExpiry,
      )));
      expect(find.text('Nothing is booked until you confirm.'), findsOneWidget);
      expect(find.text('Quoted total LKR 32,000'), findsOneWidget);
      await tester.tap(find.text('Confirm booking'));
      expect(confirmed, isTrue);

      await tester.pumpWidget(_scrollable(BookingProposalCard(proposal: proposal, now: beforeExpiry)));
      expect(find.text('Confirm booking'), findsNothing);
      expect(find.text('Superseded'), findsOneWidget);

      await tester.pumpWidget(_scrollable(BookingProposalCard(proposal: proposal, canConfirm: true, now: DateTime.utc(2026, 10, 1, 13))));
      expect(find.text('Confirm booking'), findsNothing);
      expect(find.text('Expired'), findsOneWidget);
    });

    testWidgets('a created booking shows the status returned by the backend', (tester) async {
      final message = ChatMessage.fromJson({
        ...chatResponseJson(status: 'booking_created', message: 'Booking #42 was created.', booking: bookingJson()),
        'role': 'assistant',
      });
      await tester.pumpWidget(_scrollable(ChatBubble(message: message)));

      expect(find.text('Booking #42'), findsOneWidget);
      expect(find.text('Pending'), findsOneWidget);
      expect(find.text('Confirmed'), findsNothing);
      expect(find.text('The provider still has to confirm this booking.'), findsOneWidget);
    });

    testWidgets('a refusal shows no plan or booking controls', (tester) async {
      const message = ChatMessage(
        role: 'assistant',
        content: 'I can only help with travel planning on this platform.',
        status: 'refused',
      );
      await tester.pumpWidget(_scrollable(const ChatBubble(message: message)));

      expect(find.text('Request declined'), findsOneWidget);
      expect(find.byType(TravelPlanCard), findsNothing);
      expect(find.text('Confirm booking'), findsNothing);
    });
  });

  group('Sign-in and registration', () {
    testWidgets('login rejects a malformed email before calling the API', (tester) async {
      var requests = 0;
      final auth = AuthProvider(AuthService(api: fakeApi((_) async {
        requests++;
        return jsonResponse(authJson());
      })));
      await tester.pumpWidget(ChangeNotifierProvider.value(value: auth, child: const MaterialApp(home: LoginScreen())));

      await tester.enterText(find.widgetWithText(TextFormField, 'Email'), 'nimal.example.com');
      await tester.enterText(find.widgetWithText(TextFormField, 'Password'), 'Travel#2026');
      await tester.tap(find.text('Sign In'));
      await tester.pump();

      expect(find.text('Enter a valid email address'), findsOneWidget);
      expect(requests, 0);
    });

    testWidgets('a staff account sees the travelers-only message', (tester) async {
      final auth = AuthProvider(AuthService(api: fakeApi((_) async => jsonResponse(authJson(role: 'HOTEL_OWNER')))));
      await tester.pumpWidget(ChangeNotifierProvider.value(value: auth, child: const MaterialApp(home: LoginScreen())));

      await tester.enterText(find.widgetWithText(TextFormField, 'Email'), 'owner@traveladvisor.com');
      await tester.enterText(find.widgetWithText(TextFormField, 'Password'), 'Owner@123');
      await tester.tap(find.text('Sign In'));
      await tester.pumpAndSettle();

      expect(find.text(AuthService.travelersOnlyMessage), findsOneWidget);
      expect(auth.isAuthenticated, isFalse);
    });

    testWidgets('after a session expires the sign-in screen says why', (tester) async {
      var expired = false;
      final api = fakeApi((request) async {
        if (request.url.path == '/api/auth/login') return jsonResponse(authJson());
        return expired ? http.Response('', 401) : jsonResponse([]);
      });
      final auth = AuthProvider(AuthService(api: api));
      await tester.runAsync(() async {
        await auth.login('nimal@example.com', 'Travel#2026');
        expired = true;
        await api.get('/api/bookings');
      });

      await tester.pumpWidget(ChangeNotifierProvider.value(value: auth, child: const MaterialApp(home: LoginScreen())));

      expect(find.text(AuthProvider.sessionExpiredMessage), findsOneWidget);
    });

    testWidgets('registration enforces the password policy and confirmation', (tester) async {
      final auth = AuthProvider(AuthService(api: fakeApi((_) async => jsonResponse(authJson()))));
      await tester.pumpWidget(ChangeNotifierProvider.value(value: auth, child: const MaterialApp(home: RegisterScreen())));

      await tester.enterText(find.widgetWithText(TextFormField, 'First Name'), 'Nimal');
      await tester.enterText(find.widgetWithText(TextFormField, 'Last Name'), 'Perera');
      await tester.enterText(find.widgetWithText(TextFormField, 'Email'), 'nimal@example.com');
      await tester.enterText(find.widgetWithText(TextFormField, 'Password'), 'abcdef1');
      await tester.enterText(find.widgetWithText(TextFormField, 'Confirm Password'), 'abcdef2');
      await tester.ensureVisible(find.text('Register'));
      await tester.tap(find.text('Register'));
      await tester.pump();

      expect(find.text('Password needs an uppercase letter, a symbol'), findsOneWidget);
      expect(find.text('Passwords do not match'), findsOneWidget);
      expect(auth.isAuthenticated, isFalse);
    });
  });

  group('Reviews', () {
    testWidgets('the review sheet requires a rating and submits it with the comment', (tester) async {
      (int, String)? submitted;
      bool? result;
      await tester.pumpWidget(MaterialApp(
        home: Builder(
          builder: (context) => Scaffold(
            body: Center(
              child: ElevatedButton(
                onPressed: () async {
                  result = await showModalBottomSheet<bool>(
                    context: context,
                    isScrollControlled: true,
                    builder: (_) => WriteReviewSheet(
                      title: 'Ella Gap View Inn',
                      onSubmit: (rating, comment) async => submitted = (rating, comment),
                    ),
                  );
                },
                child: const Text('Open'),
              ),
            ),
          ),
        ),
      ));
      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Submit review'));
      await tester.pump();
      expect(find.text('Choose a rating from 1 to 5 stars'), findsOneWidget);
      expect(submitted, isNull);

      await tester.tap(find.byTooltip('4 stars'));
      await tester.enterText(find.widgetWithText(TextFormField, 'Comment (optional)'), 'Lovely view');
      await tester.tap(find.text('Submit review'));
      await tester.pumpAndSettle();

      expect(submitted, (4, 'Lovely view'));
      expect(result, isTrue);
    });

    testWidgets('visible reviews are listed with the average rating', (tester) async {
      final reviews = [
        Review(id: 1, bookingId: 12, rating: 5, comment: 'Amazing views', authorName: 'Nimal P.', createdAt: DateTime.utc(2026, 9, 20)),
        Review(id: 2, bookingId: 13, rating: 4, authorName: 'Sara K.', createdAt: DateTime.utc(2026, 9, 22)),
      ];
      await tester.pumpWidget(_scrollable(ReviewsSection(load: () async => reviews, averageRating: 4.5, reviewCount: 2)));
      await tester.pumpAndSettle();

      expect(find.text('4.5 / 5 (2 reviews)'), findsOneWidget);
      expect(find.text('Nimal P.'), findsOneWidget);
      expect(find.text('Amazing views'), findsOneWidget);
      expect(find.text('Sara K.'), findsOneWidget);
    });

    testWidgets('an unreviewed listing says so', (tester) async {
      await tester.pumpWidget(_scrollable(ReviewsSection(load: () async => const <Review>[])));
      await tester.pumpAndSettle();
      expect(find.text('No reviews yet.'), findsOneWidget);
    });
  });

  group('Bookings', () {
    Future<void> pumpBookingFlow(WidgetTester tester, Future<http.Response> Function(http.Request) handler) async {
      // A phone-height viewport so the whole form, including the lazily built
      // submit button, is laid out.
      tester.view.physicalSize = const Size(800, 1600);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.reset);
      final router = GoRouter(routes: [
        GoRoute(path: '/', builder: (_, __) => CreateBookingScreen(roomId: 7, service: BookingService(fakeApi(handler)))),
        GoRoute(path: '/bookings', builder: (_, __) => const Scaffold(body: Text('Bookings page'))),
      ]);
      await tester.pumpWidget(MaterialApp.router(routerConfig: router));
    }

    FilledButton requestButton(WidgetTester tester) =>
        tester.widget<FilledButton>(find.byWidgetPredicate((w) => w is FilledButton && w.child is Text && (w.child as Text).data!.startsWith('Request booking')));

    testWidgets('a room is booked only after a successful availability check', (tester) async {
      final requests = <http.Request>[];
      await pumpBookingFlow(tester, (request) async {
        requests.add(request);
        if (request.url.path == '/api/bookings/availability') {
          return jsonResponse({'available': true, 'nights': 3, 'guests': 2, 'totalPrice': 48000, 'checkIn': '', 'checkOut': ''});
        }
        return jsonResponse(bookingJson(id: 77), 201);
      });

      expect(requestButton(tester).onPressed, isNull);

      await tester.enterText(find.widgetWithText(TextFormField, 'Guests'), '0');
      await tester.tap(find.text('Check availability'));
      await tester.pump();
      expect(find.text('At least 1 guest is required'), findsOneWidget);
      expect(requests, isEmpty);

      await tester.enterText(find.widgetWithText(TextFormField, 'Guests'), '2');
      await tester.tap(find.text('Check availability'));
      await tester.pumpAndSettle();

      expect(requests.single.url.queryParameters['roomId'], '7');
      expect(requests.single.url.queryParameters['guests'], '2');
      expect(requests.single.url.queryParameters['checkOut'], isNotNull);
      expect(find.text('Available'), findsOneWidget);
      expect(find.text('Total LKR 48,000'), findsOneWidget);
      expect(requestButton(tester).onPressed, isNotNull);

      await tester.ensureVisible(find.text('Request booking for LKR 48,000'));
      await tester.tap(find.text('Request booking for LKR 48,000'));
      await tester.pumpAndSettle();

      final body = jsonDecode(requests.last.body) as Map<String, dynamic>;
      expect(requests.last.method, 'POST');
      expect(body['roomId'], 7);
      expect(body['guests'], 2);
      expect(body['checkOut'], isNotNull);
      expect(find.text('Bookings page'), findsOneWidget);
    });

    testWidgets('changing the inputs after a quote requires a new check', (tester) async {
      await pumpBookingFlow(tester, (request) async => jsonResponse({'available': true, 'nights': 3, 'guests': 1, 'totalPrice': 30000}));

      await tester.tap(find.text('Check availability'));
      await tester.pumpAndSettle();
      expect(requestButton(tester).onPressed, isNotNull);

      await tester.enterText(find.widgetWithText(TextFormField, 'Guests'), '3');
      await tester.pump();
      expect(find.text('Available'), findsNothing);
      expect(requestButton(tester).onPressed, isNull);
    });

    testWidgets('an unavailable room shows the server reason and cannot be booked', (tester) async {
      await pumpBookingFlow(
        tester,
        (_) async => jsonResponse({'detail': 'Room is already booked for the selected dates.'}, 409),
      );

      await tester.tap(find.text('Check availability'));
      await tester.pumpAndSettle();

      expect(find.text('Room is already booked for the selected dates.'), findsOneWidget);
      expect(requestButton(tester).onPressed, isNull);
    });

    testWidgets('completed bookings can be reviewed once; open bookings can be cancelled', (tester) async {
      final api = fakeApi((request) async {
        if (request.url.path == '/api/reviews') {
          return jsonResponse([
            {'id': 1, 'bookingId': 41, 'rating': 5, 'authorName': 'Nimal P.', 'createdAt': '2026-09-20T00:00:00Z'},
          ]);
        }
        return jsonResponse([
          bookingJson(id: 40, status: 3),
          bookingJson(id: 41, status: 3, travelPackageId: 9),
          bookingJson(id: 42, status: 0),
          bookingJson(id: 43, status: 2),
        ]);
      });
      final auth = AuthProvider(AuthService(api: api));
      await tester.pumpWidget(ChangeNotifierProvider.value(value: auth, child: const MaterialApp(home: BookingListScreen())));
      await tester.pumpAndSettle();

      expect(find.text('Write a review'), findsOneWidget);
      expect(find.text('Reviewed'), findsOneWidget);
      expect(find.text('Cancel booking'), findsOneWidget);
      expect(find.text('Cancelled'), findsOneWidget);
    });

    testWidgets('cancelling refreshes the list with the status from the API', (tester) async {
      var status = 0;
      final requests = <String>[];
      final api = fakeApi((request) async {
        requests.add('${request.method} ${request.url.path}');
        if (request.url.path == '/api/reviews') return jsonResponse([]);
        if (request.method == 'PATCH') {
          status = (jsonDecode(request.body) as Map<String, dynamic>)['status'] as int;
          return jsonResponse(bookingJson(id: 42, status: status));
        }
        return jsonResponse([bookingJson(id: 42, status: status)]);
      });
      final auth = AuthProvider(AuthService(api: api));
      await tester.pumpWidget(ChangeNotifierProvider.value(value: auth, child: const MaterialApp(home: BookingListScreen())));
      await tester.pumpAndSettle();
      expect(find.text('Pending'), findsOneWidget);

      await tester.tap(find.text('Cancel booking'));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(FilledButton, 'Cancel booking'));
      await tester.pumpAndSettle();

      expect(requests, contains('PATCH /api/bookings/42/status'));
      expect(status, Booking.cancelled);
      expect(find.text('Cancelled'), findsOneWidget);
      expect(find.text('Pending'), findsNothing);
      expect(find.text('Cancel booking'), findsNothing);
    });
  });

  group('Search', () {
    testWidgets('keywords and a maximum price are sent to the API', (tester) async {
      final searches = <CatalogSearch>[];
      await tester.pumpWidget(MaterialApp(
        home: Scaffold(
          body: SearchableCatalogList<String>(
            load: (search) async {
              searches.add(search);
              return search.isEmpty ? ['Ella Gap View Inn', 'Galle Fort Hotel'] : <String>[];
            },
            hint: 'Hotel, city or country',
            priceLabel: 'Max / night',
            emptyIcon: Icons.hotel,
            emptyTitle: 'No hotels yet',
            emptyMessage: 'Approved hotels will show up here.',
            noMatchTitle: 'No hotels match your search',
            itemBuilder: (_, name) => Text(name),
          ),
        ),
      ));
      await tester.pumpAndSettle();
      expect(find.text('Galle Fort Hotel'), findsOneWidget);
      expect(searches.single.isEmpty, isTrue);

      await tester.enterText(find.widgetWithText(TextFormField, 'Max / night'), 'abc');
      await tester.tap(find.byTooltip('Search'));
      await tester.pump();
      expect(find.text('Enter a positive amount'), findsOneWidget);
      expect(searches, hasLength(1));

      await tester.enterText(find.byType(TextFormField).first, 'Kandy');
      await tester.enterText(find.widgetWithText(TextFormField, 'Max / night'), '15000');
      await tester.tap(find.byTooltip('Search'));
      await tester.pumpAndSettle();

      expect(searches.last.query, 'Kandy');
      expect(searches.last.maxPrice, 15000);
      expect(find.text('No hotels match your search'), findsOneWidget);

      await tester.tap(find.byTooltip('Clear search'));
      await tester.pumpAndSettle();
      expect(searches.last.isEmpty, isTrue);
      expect(find.text('Ella Gap View Inn'), findsOneWidget);
    });
  });
}
