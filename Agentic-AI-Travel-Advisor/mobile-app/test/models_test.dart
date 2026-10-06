import 'package:flutter_test/flutter_test.dart';
import 'package:travel_advisor/models/booking.dart';
import 'package:travel_advisor/models/chat_message.dart';
import 'package:travel_advisor/models/hotel.dart';
import 'package:travel_advisor/models/transportation.dart';
import 'package:travel_advisor/models/travel_plan.dart';
import 'package:travel_advisor/utils/format.dart';
import 'package:travel_advisor/utils/validators.dart';

import 'helpers.dart';

void main() {
  group('formatMoney', () {
    test('formats like the web app', () {
      expect(formatMoney(16000), 'LKR 16,000');
      expect(formatMoney(10368.5), 'LKR 10,368.50');
      expect(formatMoney(999), 'LKR 999');
      expect(formatMoney(1234567.891), 'LKR 1,234,567.89');
      expect(formatMoney(0), 'LKR 0');
      expect(formatMoney(-2500), 'LKR -2,500');
      expect(formatMoney(50, 'USD'), 'USD 50');
    });
  });

  group('TravelPlan', () {
    final plan = TravelPlan.fromJson(planJson());

    test('parses the structured plan returned by the assistant', () {
      expect(plan.destination, 'Ella');
      expect(plan.travelers, 2);
      expect(plan.withinBudget, isTrue);
      expect(plan.accommodationCost, 32000);
      expect(plan.transportationCost, 8000);
      expect(plan.selectedHotel?.name, 'Ella Gap View Inn');
      expect(plan.selectedPackage?.remainingPlaces, 6);
      expect(plan.selectedTransport.single.mode, 'Train');
      expect(plan.activities.single.includedInCost, isTrue);
      expect(plan.itinerary, hasLength(2));
      expect(plan.warnings, ['Trains fill up quickly on weekends.']);
    });

    test('maps to an itinerary request the same way as the web app', () {
      final request = plan.toItineraryRequest();

      expect(request['title'], 'Ella trip');
      expect(request['startDate'], '2026-10-10');
      expect(request['destinationId'], 4);
      expect(request['estimatedCost'], 76000);
      expect(request['summary'], '2 travelers, 3 days · Ella Gap View Inn + Ella Hill Country Escape');
      expect(request['travelers'], 2);
      expect(request['budget'], plan.budget);
      expect(request['conversationId'], isNull);
      expect(plan.toItineraryRequest(conversationId: 7)['conversationId'], 7);
      final items = request['items'] as List;
      expect(items, hasLength(3));
      expect(items[0], {
        'dayNumber': 1,
        'title': 'Train Kandy to Ella',
        'description': 'Scenic route',
        'startTime': '08:47:00',
        'sortOrder': 0,
      });
      expect((items[1] as Map)['startTime'], isNull, reason: '"Evening" is not a clock time');
      expect((items[1] as Map)['sortOrder'], 1);
      expect((items[2] as Map)['dayNumber'], 2);
      expect((items[2] as Map)['sortOrder'], 0);
    });

    test('tolerates missing optional sections', () {
      final minimal = TravelPlan.fromJson({'destination': 'Galle', 'travelers': 1, 'duration': 1});
      expect(minimal.hotels, isEmpty);
      expect(minimal.selectedHotel, isNull);
      expect(minimal.toItineraryRequest()['summary'], '1 traveler, 1 days');
    });
  });

  group('BookingProposal', () {
    test('expires at the time given by the backend', () {
      final expiresAt = DateTime.utc(2026, 10, 1, 12);
      final proposal = BookingProposal.fromJson(proposalJson(expiresAt: expiresAt));
      expect(proposal.isExpired(DateTime.utc(2026, 10, 1, 11, 59)), isFalse);
      expect(proposal.isExpired(DateTime.utc(2026, 10, 1, 12)), isTrue);
    });
  });

  group('ChatMessage', () {
    test('reads a booking_created reply with the backend booking', () {
      final message = ChatMessage.fromJson({
        ...chatResponseJson(status: 'booking_created', message: 'Booking #42 was created.', booking: bookingJson()),
        'role': 'assistant',
      });
      expect(message.status, 'booking_created');
      expect(message.content, 'Booking #42 was created.');
      expect(message.bookingId, 42);
      expect(message.booking?.statusLabel, 'Pending');
    });

    test('reads stored history with a plan and a proposal', () {
      final message = ChatMessage.fromJson({
        'role': 'assistant',
        'content': 'Here is your plan.',
        'status': 'booking_proposal',
        'plan': planJson(),
        'pendingBooking': proposalJson(),
        'bookingId': null,
      });
      expect(message.plan?.destination, 'Ella');
      expect(message.pendingBooking?.id, 'prop-123');
      expect(message.suggestedPlan, isNull);
    });
  });

  group('catalog models', () {
    test('booking reads guests and status constants', () {
      final booking = Booking.fromJson(bookingJson(status: Booking.completed));
      expect(booking.guests, 2);
      expect(booking.statusLabel, 'Completed');
      expect(booking.title, 'Ella Gap View Inn');
    });

    test('hotel reads rating and starting price', () {
      final hotel = Hotel.fromJson({
        'id': 3,
        'name': 'Ella Gap View Inn',
        'address': 'Passara Rd',
        'city': 'Ella',
        'country': 'Sri Lanka',
        'roomCount': 4,
        'approvalStatus': 1,
        'minPricePerNight': 12500,
        'averageRating': 4.5,
        'reviewCount': 2,
      });
      expect(hotel.minPricePerNight, 12500);
      expect(hotel.averageRating, 4.5);
      expect(hotel.reviewCount, 2);
    });

    test('transport mode numbers map to readable labels', () {
      final train = Transportation.fromJson({
        'id': 1,
        'mode': 1,
        'fromLocation': 'Kandy',
        'toLocation': 'Ella',
        'departureTime': '08:47:00',
        'durationMinutes': 400,
        'pricePerPerson': 2000,
        'capacity': 40,
      });
      expect(train.modeLabel, 'Train');
      expect(train.departureLabel, '08:47');
      expect(formatDuration(train.durationMinutes), '6 h 40 min');
    });
  });

  group('Validators', () {
    test('email', () {
      expect(Validators.email(''), 'Email is required');
      expect(Validators.email('not-an-email'), 'Enter a valid email address');
      expect(Validators.email('nimal@example'), 'Enter a valid email address');
      expect(Validators.email(' nimal@example.com '), isNull);
    });

    test('new password follows the API policy', () {
      expect(Validators.newPassword('abc'), contains('at least 6 characters'));
      expect(Validators.newPassword('abcdef1!'), 'Password needs an uppercase letter');
      expect(Validators.newPassword('ABCDEF1!'), 'Password needs a lowercase letter');
      expect(Validators.newPassword('Abcdefg!'), 'Password needs a number');
      expect(Validators.newPassword('Abcdef12'), 'Password needs a symbol');
      expect(Validators.newPassword('Travel#2026'), isNull);
    });

    test('guests', () {
      expect(Validators.guests(''), 'Enter the number of guests');
      expect(Validators.guests('0'), 'At least 1 guest is required');
      expect(Validators.guests('51'), 'At most 50 guests');
      expect(Validators.guests('2'), isNull);
    });

    test('lengths and prices', () {
      expect(Validators.maxLength('a' * 2001, 2000, 'Comment'), 'Comment must be at most 2000 characters');
      expect(Validators.maxLength('a' * 2000, 2000, 'Comment'), isNull);
      expect(Validators.optionalPrice(''), isNull);
      expect(Validators.optionalPrice('-5'), 'Enter a positive amount');
      expect(Validators.optionalPrice('abc'), 'Enter a positive amount');
      expect(Validators.optionalPrice('15000'), isNull);
    });
  });
}
