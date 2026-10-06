import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:travel_advisor/providers/chat_provider.dart';

import 'helpers.dart';

void main() {
  group('ChatProvider booking flow', () {
    late List<Map<String, dynamic>> chatRequests;

    setUp(() => chatRequests = []);

    http.Response chatHandler(Map<String, dynamic> body) {
      chatRequests.add(body);
      if (body['confirmBookingId'] == null) {
        return jsonResponse(chatResponseJson(
          status: 'booking_proposal',
          message: 'Here is a 3-day Ella plan within your budget. Confirm to book the room.',
          plan: planJson(),
          pendingBooking: proposalJson(),
        ));
      }
      return jsonResponse(chatResponseJson(
        status: 'booking_created',
        message: 'Booking #42 was created and is pending provider confirmation.',
        booking: bookingJson(),
      ));
    }

    test('a booking is only created when the user confirms the latest proposal', () async {
      final api = fakeApi((request) async => chatHandler(jsonDecode(request.body) as Map<String, dynamic>));
      final chat = ChatProvider();

      await chat.send(api, 'Plan 3 days in Ella for 2 under LKR 80,000');

      expect(chatRequests, hasLength(1));
      expect(chatRequests.single.containsKey('confirmBookingId'), isFalse);
      final proposalMessage = chat.messages.last;
      expect(proposalMessage.plan?.destination, 'Ella');
      expect(chat.canConfirm(proposalMessage), isTrue);

      await chat.confirm(api, proposalMessage.pendingBooking!);

      expect(chatRequests, hasLength(2));
      expect(chatRequests[1]['confirmBookingId'], 'prop-123');
      expect(chatRequests[1]['conversationId'], 7);
      expect(chatRequests[1]['message'], startsWith('Confirm booking: '));
      final reply = chat.messages.last;
      expect(reply.status, 'booking_created');
      expect(reply.booking?.statusLabel, 'Pending', reason: 'the app shows what the backend returned');
      expect(chat.canConfirm(proposalMessage), isFalse, reason: 'the proposal has been superseded');
    });

    test('an expired proposal cannot be confirmed', () async {
      final api = fakeApi((_) async => jsonResponse(chatResponseJson(
            status: 'booking_proposal',
            message: 'Confirm to book.',
            pendingBooking: proposalJson(expiresAt: DateTime.now().toUtc().subtract(const Duration(minutes: 1))),
          )));
      final chat = ChatProvider();
      await chat.send(api, 'Book the room');

      expect(chat.canConfirm(chat.messages.last), isFalse);
    });

    test('a failed request is shown as an error and never as a booking', () async {
      final api = fakeApi((_) async => jsonResponse({'detail': 'Too many requests. Please slow down.'}, 429));
      final chat = ChatProvider();
      await chat.send(api, 'Plan a trip');

      expect(chat.error, 'Too many requests. Please slow down.');
      expect(chat.messages.last.status, 'error');
      expect(chat.messages.last.bookingId, isNull);
    });

    test('saving the latest structured plan posts the itinerary mapping', () async {
      Map<String, dynamic>? itineraryBody;
      final api = fakeApi((request) async {
        if (request.url.path == '/api/itineraries') {
          itineraryBody = jsonDecode(request.body) as Map<String, dynamic>;
          return jsonResponse({
            'id': 11,
            'title': 'Ella trip',
            'startDate': '2026-10-10T00:00:00Z',
            'endDate': '2026-10-12T00:00:00Z',
            'createdAt': '2026-10-01T10:00:00Z',
            'items': [],
          }, 201);
        }
        return chatHandler(jsonDecode(request.body) as Map<String, dynamic>);
      });
      final chat = ChatProvider();
      await chat.send(api, 'Plan 3 days in Ella');

      final saved = await chat.saveLatestPlan(api);

      expect(saved?.id, 11, reason: chat.error);
      expect(itineraryBody?['title'], 'Ella trip');
      expect((itineraryBody?['items'] as List).first['startTime'], '08:47:00');
      expect(chat.savedItineraryId, 11);
    });

    test('reset clears every trace of the conversation', () async {
      final api = fakeApi((request) async => chatHandler(jsonDecode(request.body) as Map<String, dynamic>));
      final chat = ChatProvider();
      await chat.send(api, 'Plan a trip');

      chat.reset();

      expect(chat.hasState, isFalse);
      expect(chat.latestPlanMessage, isNull);
    });
  });
}
